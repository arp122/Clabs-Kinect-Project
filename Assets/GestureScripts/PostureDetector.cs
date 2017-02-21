using System;

/// detect postures
public class PostureDetector
{
	public string m_name = ConstDef.defaultPostName;

	public JointMatcher[] m_jointMatcher = null;

	/****************************** above are initialized from xml file or externally+manually ********************************/

	/// the count of joints we need to check in the template, not the count of all the joints
	public int JointCount { get { return m_jointMatcher.Length; } }

	/// the matching score of each joint
	private float[] m_jointScores;
	public float[] JointScore { get { return m_jointScores; } }

	/// the matching score of the posture
	private float m_postScore = 0;
	public float Score { get { return m_postScore; } }

	private bool m_isRising;

	private bool m_isDetected;
	public bool IsDetected { get { return m_isDetected; } }
	
	
	public JointMatcher this[int jointNo]
	{
		get
		{
			return m_jointMatcher[jointNo];
		}

		set
		{
			m_jointMatcher[jointNo] = value;
		}
	}

	public PostureDetector()
	{
	}

	// called in GestureDetector::Init
	// the initialization of public members should be done by deserialzation of .xml files or externally+manually
	public void Init(JointFeature pf)
	{
		if (m_jointMatcher == null)
		{
			throw new Exception("the JointMatcher members should be initialized first.");
		}
		m_jointScores = new float[JointCount];
		for (int i = 0; i < JointCount; i++)
		{
			m_jointMatcher[i].Init(pf);
		}

	}

	/// @param isCheckRise if true, method returns true only when the score starts to fall.
	/// if false, method returns true as long as all joint matches.
	public bool Detect(bool isCheckRise)
	{
		m_isDetected = true;

		for (int i = 0; i < JointCount; i++)
		{
			if (!m_jointMatcher[i].Match())
				m_isDetected = false;
			m_jointScores[i] = m_jointMatcher[i].GetMatchScore();
		}
		if (isCheckRise && m_isDetected)
		{
			float newScore = Stat.Avg(m_jointScores);
			m_isDetected = (m_isRising && newScore <= m_postScore); // starts to fall
			m_isRising = (newScore > m_postScore);
			m_postScore = newScore;
		}

		if (m_isDetected)
		{
			foreach (var jm in m_jointMatcher) // for JointMatcherR
				jm.SaveLastFt();
		}
		return m_isDetected;
	}
}
