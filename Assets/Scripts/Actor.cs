using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Actor : MonoBehaviour
{
	private float[][] FrameData;
	private float FrameTime;
	private int NumFrames;
	private bool PauseFrame;

	public string BVHFilePath;
	public int CurrentFrame;
	public List<Joint> Joints;

	private void LoadBVH()
	{
		var bp = new BVHParser(File.ReadAllText(BVHFilePath));

		FrameTime = bp.frameTime;
		NumFrames = bp.frames;

		FrameData = new float[NumFrames][];
		var channels = bp.GetChannels();
		for (var frameIdx = 0; frameIdx < NumFrames; frameIdx++)
		{
			FrameData[frameIdx] = new float[channels.Length];

			for (var channelIdx = 0; channelIdx < channels.Length; channelIdx++)
			{
				FrameData[frameIdx][channelIdx] = channels[channelIdx][frameIdx];
			}
		}

		Joints = new List<Joint>();

		Func<BVHParser.BVHBone, Joint, int> recursion = null;
		recursion = (bvhBone, parentBone) => {
			bool isRoot = (parentBone == null);

			// create new joint
			Joint joint = new Joint(this);
			Joints.Add(joint);
			joint.Index = Joints.Count() - 1;
			joint.Name = bvhBone.name;

			// set joint local offset
			joint.LocalPosition = new Vector3(bvhBone.offsetX, bvhBone.offsetY, bvhBone.offsetZ);

			// compute rotation order for children
			string order = (!isRoot)
				? string.Join("", bvhBone.channelOrder.Take(3))
				: string.Join("", bvhBone.channelOrder.Skip(3).Take(3));

			switch (order)
			{
				case "345":
					joint.RotateOrder = Joint.RotationOrder.XYZ;
					break;
				case "354":
					joint.RotateOrder = Joint.RotationOrder.XZY;
					break;
				case "435":
					joint.RotateOrder = Joint.RotationOrder.YXZ;
					break;
				case "453":
					joint.RotateOrder = Joint.RotationOrder.YZX;
					break;
				case "534":
					joint.RotateOrder = Joint.RotationOrder.ZXY;
					break;
				case "543":
					joint.RotateOrder = Joint.RotationOrder.ZYX;
					break;
				default:
					joint.RotateOrder = Joint.RotationOrder.NONE;
					break;
			}

			// set joint parent
			if (!isRoot)
			{
				joint.ParentIdx = parentBone.Index;
			}

			// recursively call children
			parentBone = joint;
			foreach (var child in bvhBone.children)
			{
				var childIdx = recursion(child, parentBone);
				joint.ChildrenIdx.Add(childIdx);
			}

			return joint.Index;
		};
		recursion(bp.root, null);
	}

	private void UpdateJointsPosition()
    {
		ForwardKinematics.UpdateJointPositions(this, FrameData[CurrentFrame % NumFrames]);
    }

	public Joint GetRootJoint()
    {
		return Joints[0];
    }

	void Awake()
    {
		LoadBVH();
		Time.fixedDeltaTime = FrameTime;
	}

    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Space))
        {
			PauseFrame = !PauseFrame;
		}
	}

    void FixedUpdate()
	{
		UpdateJointsPosition();
		if (!PauseFrame)
		{
			CurrentFrame = (CurrentFrame + 1) % NumFrames;
		}
    }
}

[Serializable]
public class Joint
{
	public Actor Actor;
	public string Name;
	public int Index;
	public int ParentIdx;
	public List<int> ChildrenIdx;

	public Vector3 LocalPosition;
	public Vector3 GlobalPosition;
	public Quaternion LocalQuaternion;
	public Quaternion GlobalQuaternion;
	public RotationOrder RotateOrder;

	public enum RotationOrder
	{
		NONE, XYZ, XZY, YXZ, YZX, ZXY, ZYX
	}

	public Joint(Actor actor)
	{
		Actor = actor;
		ParentIdx = -1;
		ChildrenIdx = new List<int>();
		LocalQuaternion = Quaternion.identity;
		GlobalQuaternion = Quaternion.identity;
		RotateOrder = RotationOrder.NONE;
	}

	public Joint GetParent()
	{
		return ParentIdx == -1 ? null : Actor.Joints[ParentIdx];
	}

	public List<Joint> GetChildren()
	{
		return ChildrenIdx.ConvertAll(idx => Actor.Joints[idx]);
	}
}
