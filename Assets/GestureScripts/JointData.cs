using OpenNI;
using UnityEngine;

/// struct to store the joint data acquired from Kinect
public struct JointData
{
//	public Joint m_joint = Joint.Invalid;

	/// position
	public Vector3 m_pos;

	/// confidence of position. -1 if not initialized
	public float m_posConf;
	
	/// joint orientation acquired from OpenNI. useful when setting the 3D avatar.
	public Vector3[] m_orient;
	
	/// confidence of orientation. -1 if not initialized
	public float m_orientConf;
	
	
	/// return a new un-initialized object
	public static JointData NewObject()
	{
		JointData jd;
		jd.m_pos = Vector3.zero;
		jd.m_orient = new Vector3[3];
		for (int i = 0; i < 3; i++)
			jd.m_orient[i] = Vector3.zero;
		jd.m_posConf = -1;
		jd.m_orientConf = -1;
		return jd;
	}

	/// initialize by only position
	public JointData(Vector3 pos):this()
	{
		m_pos = pos;
		m_orient = new Vector3[3];
		m_posConf = 1;
		m_orientConf = 1;
	}

	/// copy constructor
	public JointData(JointData old)
	{
		this = old;
		this.m_orient = (Vector3[])old.m_orient.Clone();
	}

	/// initialize from OpenNI data
	public JointData (SkeletonJointTransformation k):this()
	{
//		m_joint = joint;
		FromRaw(k);
	}

	/// initialize from OpenNI data
	public void FromRaw(SkeletonJointTransformation k)
	{
//		m_joint = joint;
		Point3D p = k.Position.Position;
		m_pos.x = p.X;
		m_pos.y = p.Y;
		m_pos.z = 1500-p.Z; // the z axis of the (Kinect) sensor is opposite to the one in Unity3D. 
		// 1500 is a arbitrary offset to make the value around 0.
		m_posConf = k.Position.Confidence;
		SkeletonJointOrientation o = k.Orientation;
		m_orient = new Vector3[3];
		m_orient[0].x = o.X1; m_orient[0].y = o.X2; m_orient[0].z = -o.X3; // the z axis of the sensor is opposite to the one in Unity3D
		m_orient[1].x = o.Y1; m_orient[1].y = o.Y2; m_orient[1].z = -o.Y3;
		m_orient[2].x = o.Z1; m_orient[2].y = o.Z2; m_orient[2].z = -o.Z3;
		m_orientConf = o.Confidence;
	}
	
}
