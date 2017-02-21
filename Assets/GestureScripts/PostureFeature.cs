using System;
using System.Collections.Generic;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// calculate the feature (orientation) for all joints
public class JointFeature
{
	/// only calculate the feature of the joints in this list
	public List<Joint> m_jointsNeedUpdate;
	
	/// the calculated feature
	public Vector3[] m_jointVec;
	
	/// the data source
	public JointData[] m_jointData;
	
	public static readonly int m_totalJointCount;

	/// m_jointStartIdx[i] is the joint "before" the joint (Joint)i
	private static readonly Joint[] m_jointStartIdx;

	/// m_jointEndIdx[i] is the joint "after" the joint (Joint)i
	private static readonly Joint[] m_jointEndIdx;
	
	
	/// initialize static members
	static JointFeature ()
	{
		//m_typeCount = Enum.GetNames(typeof(FeatureType)).Length;
        m_totalJointCount = Enum.GetNames(typeof(Joint)).Length + 1; // Enum starts at 1
		m_jointStartIdx = new Joint[m_totalJointCount];
		m_jointEndIdx = new Joint[m_totalJointCount];
		InitJointVecIdx();
	}

	/// assign m_jointStartIdx and m_jointEndIdx
	private static void InitJointVecIdx()
	{
		Joint head = Joint.Head,
			neck = Joint.Neck,			tors = Joint.Torso,
			lShd = Joint.LeftShoulder,	lElb = Joint.LeftElbow,		lHnd = Joint.LeftHand,
			lHip = Joint.LeftHip,		lKne = Joint.LeftKnee,		lFot = Joint.LeftFoot,
			rShd = Joint.RightShoulder,	rElb = Joint.RightElbow,	rHnd = Joint.RightHand,
			rHip = Joint.RightHip,		rKne = Joint.RightKnee,		rFot = Joint.RightFoot;
		Joint[] startJointIdx = 	{head,neck,lShd,tors,lHip,neck,rShd,tors,rHip};
		Joint[] currentJointIdx =	{neck,lShd,lElb,lHip,lKne,rShd,rElb,rHip,rKne};
		Joint[] endJointIdx =		{tors,lElb,lHnd,lKne,lFot,rElb,rHnd,rKne,rFot};
		for(int i = 0; i < currentJointIdx.Length; i++)
		{
			m_jointStartIdx[(int)currentJointIdx[i]] = startJointIdx[i];
			m_jointEndIdx[(int)currentJointIdx[i]] = endJointIdx[i];
		}
	}
	
	public JointFeature (JointData[] jointData)
	{
		m_jointData = jointData;
		Reset();
	}
	
	public void Reset()
	{
		m_jointsNeedUpdate = new List<Joint>();
		m_jointVec = new Vector3[m_totalJointCount];
	}

	/// declare that Joints in jointIdx needs to be updated
	public void RegisterJoints(Joint[] jointIdx)
	{
		foreach(Joint j in jointIdx)
		{
			RegisterJoint(j);
		}
	}

	public void RegisterJoint(Joint j)
	{
		if (!m_jointsNeedUpdate.Contains(j))
			m_jointsNeedUpdate.Add(j);
	}

	public void UpdateFeatures()
	{
		// adjust the orientation of the body, make the posture detections robust to the body's orientation
		int rs = (int)Joint.RightShoulder, ls = (int)Joint.LeftShoulder, ts = (int)Joint.Torso;
		Vector3 horizon = m_jointData[rs].m_pos - m_jointData[ls].m_pos,
			vertical = (m_jointData[rs].m_pos + m_jointData[ls].m_pos)/2 - m_jointData[ts].m_pos;
		Vector3 userZ = Vector3.Cross(horizon, vertical); // cross is according to "left hand rule". This is the backward direction of the user
		Quaternion rotToStraight = Quaternion.Inverse(Quaternion.LookRotation(userZ, vertical));		

		foreach(Joint j in m_jointsNeedUpdate)
		{
			int curJointIdx = (int)j;
			int endJointIdx = (int)m_jointEndIdx[curJointIdx];
			Vector3 rawEndVec = m_jointData[endJointIdx].m_pos - m_jointData[curJointIdx].m_pos; // the raw feature
			m_jointVec[curJointIdx] = (rotToStraight * rawEndVec).normalized; // adjust and normalize
		}
	}

	/// if any Joint in jointIdx is not confident (including the Joint "after" it), return false
	public bool CheckPositionConfidence(Joint[] jointIdx)
	{
		foreach(Joint j in jointIdx)
		{
			int curJointIdx = (int)j;
			int endJointIdx = (int)m_jointEndIdx[curJointIdx];
			if(m_jointData[curJointIdx].m_posConf < ConstDef.minConfidence ||
				m_jointData[endJointIdx].m_posConf < ConstDef.minConfidence)
			{
				return false;
			}
		}
		return true;
	}
				
}
