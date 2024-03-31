//Sihui Wang 301474102 (swa279)

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class ForwardKinematics
{
    public static void UpdateJointPositions(Actor actor, float[] frameData)
    {
        /*** Please write your code here ***/
        /*** code to be completed by students begins ***/
        Joint rt=actor.GetRootJoint();
        int pos=0;
        float x=frameData[pos++];
        float y=frameData[pos++];
        float z=frameData[pos++];
        rt.LocalPosition=new Vector3(x,y,z);
        Func<Joint, int> UpdateLocalRotationRecursive = null;
        UpdateLocalRotationRecursive = (node) =>{
            float rx=0.0f;
            float ry=0.0f;
            float rz=0.0f;
            if(node.RotateOrder == Joint.RotationOrder.NONE){
                return node.Index;
            }
            else if(node.RotateOrder == Joint.RotationOrder.XYZ){
                rx=frameData[pos++];
                ry=frameData[pos++];
                rz=frameData[pos++];
                Quaternion X=Quaternion.Euler(rx,0,0);
                Quaternion Y=Quaternion.Euler(0,ry,0);
                Quaternion Z=Quaternion.Euler(0,0,rz);
                node.LocalQuaternion=X*Y*Z;
            }
            else if(node.RotateOrder == Joint.RotationOrder.XZY){
                rx=frameData[pos++];
                rz=frameData[pos++];
                ry=frameData[pos++];
                Quaternion X=Quaternion.Euler(rx,0,0);
                Quaternion Y=Quaternion.Euler(0,ry,0);
                Quaternion Z=Quaternion.Euler(0,0,rz);
                node.LocalQuaternion=X*Z*Y;
            }
            else if(node.RotateOrder == Joint.RotationOrder.YXZ){
                ry=frameData[pos++];
                rx=frameData[pos++];
                rz=frameData[pos++];
                Quaternion X=Quaternion.Euler(rx,0,0);
                Quaternion Y=Quaternion.Euler(0,ry,0);
                Quaternion Z=Quaternion.Euler(0,0,rz);
                node.LocalQuaternion=Y*X*Z;
            }
            else if(node.RotateOrder == Joint.RotationOrder.YZX){
                ry=frameData[pos++];
                rz=frameData[pos++];
                rx=frameData[pos++];
                Quaternion X=Quaternion.Euler(rx,0,0);
                Quaternion Y=Quaternion.Euler(0,ry,0);
                Quaternion Z=Quaternion.Euler(0,0,rz);
                node.LocalQuaternion=Y*Z*X;
            }
            else if(node.RotateOrder == Joint.RotationOrder.ZXY){
                rz=frameData[pos++];
                rx=frameData[pos++];
                ry=frameData[pos++];
                Quaternion X=Quaternion.Euler(rx,0,0);
                Quaternion Y=Quaternion.Euler(0,ry,0);
                Quaternion Z=Quaternion.Euler(0,0,rz);
                node.LocalQuaternion=Z*X*Y;
            }
            else if(node.RotateOrder == Joint.RotationOrder.ZYX){
                rz=frameData[pos++];
                ry=frameData[pos++];
                rx=frameData[pos++];
                Quaternion X=Quaternion.Euler(rx,0,0);
                Quaternion Y=Quaternion.Euler(0,ry,0);
                Quaternion Z=Quaternion.Euler(0,0,rz);
                node.LocalQuaternion=Z*Y*X;
            }
            List<Joint> ch=node.GetChildren();
            foreach(var child in ch){
                UpdateLocalRotationRecursive(child);
            }
            return node.Index;
        };
        UpdateLocalRotationRecursive(rt);
        Func<Joint, int> UpdateGlobalRotationRecursive = null;
        UpdateGlobalRotationRecursive = (node) =>{
            if(node.ParentIdx==-1){
                node.GlobalQuaternion=node.LocalQuaternion;
            }
            else{
                node.GlobalQuaternion=actor.Joints[node.ParentIdx].GlobalQuaternion*node.LocalQuaternion;
            }
            List<Joint> ch=node.GetChildren();
            foreach(var child in ch){
                UpdateGlobalRotationRecursive(child);
            }
            return node.Index;
        };
        UpdateGlobalRotationRecursive(rt);
        Func<Joint, int> UpdateGlobalPositionRecursive=null;
        UpdateGlobalPositionRecursive = (node) =>{
            if(node.ParentIdx==-1){
                node.GlobalPosition=node.LocalPosition;
            }
            else{
                Quaternion LocalVecQ=new Quaternion();
                LocalVecQ.w=0;
                LocalVecQ.x=node.LocalPosition.x;
                LocalVecQ.y=node.LocalPosition.y;
                LocalVecQ.z=node.LocalPosition.z;
                LocalVecQ=actor.Joints[node.ParentIdx].GlobalQuaternion*LocalVecQ*Quaternion.Inverse(actor.Joints[node.ParentIdx].GlobalQuaternion);
                Vector3 LocalVec=new Vector3(LocalVecQ.x,LocalVecQ.y,LocalVecQ.z);
                node.GlobalPosition=actor.Joints[node.ParentIdx].GlobalPosition+LocalVec;
            }
            List<Joint> ch=node.GetChildren();
            foreach(var child in ch){
                UpdateGlobalPositionRecursive(child);
            }
            return node.Index;
        };
        UpdateGlobalPositionRecursive(rt);
        /*** code to be completed by students ends ***/
    }
}
