using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActorRenderer : MonoBehaviour
{
    private Actor Actor;

    private List<Transform> Joints;
    private List<Transform> Bones;
    private float ActorSmartScale;

    public float ActorScale;
    public float BoneWidth;
    public Color BoneColor = Color.gray;
    public Color JointColor = Color.red;

    private Transform CreateJointObject(Joint joint)
    {
        Transform joint_ball = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(joint_ball.GetComponent<SphereCollider>());
        joint_ball.parent = transform;
        joint_ball.GetComponent<Renderer>().material.color = JointColor;

        return joint_ball;
    }

    private Transform CreateBoneObject(Joint joint)
    {
        Transform bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        Destroy(bone.GetComponent<CapsuleCollider>());
        bone.parent = transform;
        bone.GetComponent<Renderer>().material.color = BoneColor;

        return bone;
    }

    private void SetActorSmartScale()
    {
        ActorSmartScale = 1;
        var legJoint = Actor.Joints.Find(x => x.Name.Contains("Leg"));
        if (legJoint is not null)
        {
            ActorSmartScale = 0.5f / Vector3.Magnitude(legJoint.LocalPosition);
        }
    }

    private void InitializeSkeleton()
    {
        Joints = new List<Transform>();
        Bones = new List<Transform>();

        Joints.Add(CreateJointObject(Actor.GetRootJoint()));
        Bones.Add(null);

        foreach (Joint joint in Actor.Joints.Skip(1))
        {
            Joints.Add(CreateJointObject(joint));
            Bones.Add(CreateBoneObject(joint));
        }
    }

    private void UpdateSkeletonPosition()
    {
        var scale = ActorScale * ActorSmartScale;
        var jointLocalScale = 2f * Vector3.one * BoneWidth * ActorScale;
        if (Joints.Count > 0)
        {
            Joints[0].localScale = jointLocalScale;
            Joints[0].position = Actor.GetRootJoint().GlobalPosition * scale;
        }

        for (int i = 1; i < Actor.Joints.Count; i++)
        {
            var joint = Actor.Joints[i];

            // Update joints' position
            Joints[i].localScale = jointLocalScale;
            Joints[i].position = joint.GlobalPosition * scale;

            // Update bones' position
            var boneLength = Vector3.Magnitude(joint.LocalPosition) * ActorSmartScale;
            Bones[i].position = joint.LocalPosition;
            Bones[i].localScale = new Vector3(BoneWidth, boneLength / 2, BoneWidth) * ActorScale;
            Bones[i].position = (joint.GlobalPosition + joint.GetParent().GlobalPosition) / 2 * scale;
            Bones[i].rotation = joint.GetParent().GlobalQuaternion * Quaternion.FromToRotation(Vector3.up, joint.LocalPosition);
        }
    }

    private void Start()
    {
        /*Quaternion Z=new Quaternion();
        Z.eulerAngles=new Vector3(0,0,90);
        //print(q.x);
        //print(q.y);
        //print(q.z);
        //print(q.w);
        Quaternion X=new Quaternion();
        X.eulerAngles=new Vector3(90,0,0);
        Quaternion p=new Quaternion();
        p.w=0;
        p.x=1;
        p.y=0;
        p.z=0;
        Quaternion r=new Quaternion();
        r=X*Z*p*Quaternion.Inverse(Z)*Quaternion.Inverse(X);
        print(r.x);
        print(r.y);
        print(r.z);
        print(r.w);
        Quaternion comp=Quaternion.Euler(90,0,90);
        r=comp*p*Quaternion.Inverse(comp);
        print(r.x);
        print(r.y);
        print(r.z);
        print(r.w);*/
        Actor = GetComponent<Actor>();
        SetActorSmartScale();
        InitializeSkeleton();
    }

    private void Update()
    {
        UpdateSkeletonPosition();
    }
}
