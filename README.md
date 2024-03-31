# Forward Kinematics

## 1.Introduction

This is to reconstruct motions of a skeleton from BVH motion capture files.

In this assignment, I read hierarchical structure of the joints, and the relative motions of each part of the skeleton from the BVH file. Then, I compute each joint's position from the motion capture data to reconstruct the motion of the skeleton.

**Topic:** _Forward Kinematics_ 

**Skills:** _Unity3D, C#_

## 2.How to run the code

Open `Assets > Scenes > Assignment2.unity` with `Unity3D`. Press the `play` button to run the code.

## 3.Demo

Below is the reconstructed motion from the BVH file at `Assets > Resources > Bvh > fightAndSports1.bvh`.

![fight and sports](/Demo/766A2.gif)

Reconstruction of `Assets > Resources > Bvh > running.bvh`:

![running](/Demo/766A202.gif)

Reconstruction of `Assets > Resources > Bvh > jumping.bvh`:

![jumping](/Demo/766A203.gif)
