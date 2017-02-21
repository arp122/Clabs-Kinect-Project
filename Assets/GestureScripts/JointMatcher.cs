using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// check if the current joint orientation matches with the template; train the template
[XmlInclude(typeof(JointMatcherR))]
public class JointMatcher
{
	/// the joint index
	public Joint m_jointIdx;

	public Vector3 m_template;

	public Vector3 m_templateThreshold;

	/****************************** above are initialized from xml file or externally+manually ********************************/

	/// initialized in GestureDetector.Init
	[XmlIgnore]
	public float m_thresMul = 1;

	protected Vector3 m_realThres;

	/// a reference to PostureFeature.m_jointVec which stores orientation of all the joints
	protected Vector3[] m_jointFt;

	protected Vector3 m_distance;

	protected Vector3 m_ft;

	protected Vector3 m_lastMatchFt;
	public Vector3 LastMatchFt { get { return m_lastMatchFt; } }

	protected List<Vector3> m_trainFtList;


	public JointMatcher()
	{
		m_trainFtList = new List<Vector3>();
	}

	// called in PostureDetector::Init
	// the initialization of public members should be done by deserialzation of .xml files or externally+manually
	public void Init(JointFeature pf)
	{
		m_jointFt = pf.m_jointVec;
		m_realThres = m_thresMul * m_templateThreshold;
	}

	/// @return true if the joint orientation matches
	public virtual bool Match()
	{
		m_ft = m_jointFt[(int)m_jointIdx]; // get the feature for the joint
		Quaternion q = Quaternion.FromToRotation(m_ft, m_template);
		m_distance = q.eulerAngles.MinAngle().Abs();
		return m_distance.NotLargerThan(m_realThres);
	}

	/// @return 1 if totally the same orientations, <0 if not matched. the less, the less matched.
	public float GetMatchScore()
	{
		return 1 - V3Extension.DotDiv(m_distance, m_realThres).Max();
	}

	/// save feature for JointMatcherR
	public void SaveLastFt()
	{
		m_lastMatchFt = m_ft;
	}

	public virtual void AddTrainData(Vector3 v)
	{
		m_trainFtList.Add(v);
		m_lastMatchFt = v;
	}


	/// train the orientation template and threshold
	/// @return the scatter level of the training samples
	public virtual float Train()
	{
		Vector3 avg, std;
		Vector3[] ftArray = m_trainFtList.ToArray();
		Stat.AvgStd3Axis(ftArray, out avg, out std);
		m_template = avg;

		// the threshold is proportional to the standard deviation, but not less than ConstDef.minAngleThres
		m_templateThreshold = Vector3.Max(Vector3.one * ConstDef.minAngleThres, std * ConstDef.angleThresMul);
		Vector3 std1;
		//Stat.AvgStdAngle(ftArray, out avg, out std1); // use the standard deviation of angles between the template and training vectors
		//// as the returned scatter measure
		Stat.AvgStd3Coord(ftArray, out avg, out std1); // use the standard deviation of each coordinate between the template and training vectors
		// as the returned scatter measure

		return std1.magnitude;
	}

}
