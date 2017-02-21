using UnityEngine;

/// a JointMatcher uses the "relative" algorithm. 
/// should not be used in the first key posture.
/// the performance is not better than the original JointMatcher
public class JointMatcherR : JointMatcher
{
	private JointMatcher m_lastPostJoint;
	
	private GestureDetector m_gd;
	
	private int m_lastPostIdx;
	

	public JointMatcherR() : base()
	{
	}

	public void SetLastPostJoint(GestureDetector gd, int postIdx, int jointIdx)
	{
		m_lastPostJoint = gd[postIdx-1][jointIdx];
		m_gd = gd;
		m_lastPostIdx = postIdx-1;
	}

	/// @return true if the difference of the current feature and the feature of the same joint
	/// in the last key posture matches the template
	public override bool Match()
	{
		if(m_gd.CurState-1 != m_lastPostIdx) // state == postIdx+1
			return false;
		
		m_ft = m_jointFt[(int)m_jointIdx];
		m_distance = ((m_ft-m_lastPostJoint.LastMatchFt) - m_template).Abs();
		return m_distance.NotLargerThan(m_realThres);
	}

	public override void AddTrainData(Vector3 v)
	{
		m_trainFtList.Add(v - m_lastPostJoint.LastMatchFt);
		m_lastMatchFt = v;
	}

	public override float Train()
	{
		Vector3 avg, std;
		Vector3[] ftArray = m_trainFtList.ToArray();
		Stat.AvgStd3Coord(ftArray, out avg, out std);
		m_template = avg;
		m_templateThreshold = Vector3.Max(Vector3.one * ConstDef.minCoordThres, std * ConstDef.coordThresMul);
		// use the standard deviation of each coordinate between the template and training vectors
		// as the returned scatter measure
		return std.magnitude;
	}
}
