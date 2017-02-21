using System;
using System.IO;
using System.Xml.Serialization;
using Joint = OpenNI.SkeletonJoint;

/// detects a gesture; load and save template files
public class GestureDetector
{
	public string m_name = ConstDef.defaultGestName;

	/// the joint indices to check
	public Joint[] m_jointIdx = null;

	/// the multiplier for the thresholds in each posture
	///
	/// could be manually edited in the trained xml file to adjust the importance of each joint. default is 1
	/// the larger the value is, the larger the threshold of the joint, so the less accurate the posture could be.
	public float[] m_thresMulPerPost = null;

	/// the multiplier for the thresholds of each joint
	///
	/// could be manually edited in the trained xml file to adjust the importance of each joint. default is 1
	/// the larger the value is, the larger the threshold of the joint, so the less accurate the joint could be.
	public float[] m_thresMulPerJoint = null;

	/// key postures
	public PostureDetector[] m_posts = null;

	/// see class FSM
	public float[] m_intervals = null;

	public float[] m_intvThres = null;

	/// the multiplier for the thresholds of time intervals
	///
	/// could be manually edited in the trained xml file to adjust the threshold of the intervals. default is 1
	/// the larger the value is, the larger the threshold of the intervals, so the less accurate gesture's speed could be.
	public float m_thresMulTimeIntv = 1;

	/****************************** above are initialized from xml file or externally+manually ********************************/

	private bool[] m_isPostDet;

	/// if m_post[i] has been detected
	public bool[] IsPostDet { get {return m_isPostDet;} }

	private FSM m_fsm;
	public FSM Fsm { get { return m_fsm; } }
	
	public int CurState { get { return m_fsm.m_state; } }

	private bool m_isDetected = false;

	/// if the gesture has been detected (Detect method doesn't have to return true)
	public bool IsDetected { get { return m_isDetected; } }

	/// set the way that Detect method return true
	///
	/// if 0, Detect return true only when the gesture is detected.
	/// if >0, Detect return true every m_frameIntv frames if the user holds the last posture of the gesture.
	[XmlIgnore]
	public int m_frameIntv = 0;
	private int m_frameIntvCounter = 0;
	private bool m_isStartCont = false;

	/// Detect method wait m_frameWait frames before continue to return true 
	/// if m_frameIntv>0 and the user holds the last posture of the gesture
	[XmlIgnore]
	public int m_frameWait = 50;

	/// a reference to the public feature calculator
	private JointFeature m_postFeature;

	/// the count of key postures
	public int PostureCount { get { return m_posts.Length; } }

	/// the count of joints we need to check in the template, not the count of all the joints
	public int JointCount { get { return m_jointIdx.Length; } }

	public PostureDetector this[int postNo]
	{
		get
		{
			return m_posts[postNo];
		}
	}


	public GestureDetector() {}

	/// initializer. called after LoadFromFile is called
	public void Init(JointFeature pf)
	{
		if (m_posts == null)
		{
			throw new Exception("the PostureDetector members should be initialized first.");
		}
		m_postFeature = pf;
		m_fsm = new FSM(PostureCount, m_intervals, m_intvThres);
		m_fsm.m_thresMul = m_thresMulTimeIntv;
		m_isPostDet = new bool[PostureCount];
		m_postFeature.RegisterJoints(m_jointIdx);

		// more initialization for the relative joint matcher
		for (int i = 0; i < PostureCount; i++ )
		{
			for (int j = 0; j < m_posts[i].JointCount; j++ )
			{
				m_posts[i][j].m_thresMul = m_thresMulPerPost[i] * m_thresMulPerJoint[j];
				JointMatcherR jmr = m_posts[i][j] as JointMatcherR;
				if (jmr != null)
					jmr.SetLastPostJoint(this, i, j);
			}
			m_posts[i].Init(pf); // should be called after m_posts[i][j].m_thresMul is initialized
		}
	}

	public static GestureDetector LoadFromFile(string templateName)
	{
		using (StreamReader reader = new StreamReader(ConstDef.tmplRootPath + templateName))
		{
			XmlSerializer formatter = new XmlSerializer(typeof(GestureDetector));
			return (GestureDetector)formatter.Deserialize(reader);
		}
	}

	public void SaveToFile(StreamWriter writer)
	{
		XmlSerializer formatter = new XmlSerializer(this.GetType());
		formatter.Serialize(writer, this);
	}

	/// @return true if the joint data related with this gesture are all confident enough
	public bool CheckPositionConfidence()
	{
		return m_postFeature.CheckPositionConfidence(m_jointIdx);
	}

	public bool Detect()
	{
		//if (m_posts == null) return false; // check if the gesture has been initialized
		//for (int i = 0; i < PostureCount; i++) // all postures are updated. Maybe not efficient
		//{
		//	m_isPostDet[i] = m_posts[i].Detect();
		//}
		if (PostureCount == 1)
		{
			return DetectStatic();
		}
		else // the gesture has more than one posture
		{
			return DetectDynamic();
		}
	}
	
	private bool DetectStatic()
	{
		m_isPostDet[0] = m_posts[0].Detect(false);
		if (m_isPostDet[0])
		{
			if (!m_isDetected)
			{
				m_isDetected = true;
				return m_isDetected;
			}
			else if (m_frameIntv == 0)
				return false;
			else
			{
				m_frameIntvCounter = (m_frameIntvCounter + 1) % (m_isStartCont ? m_frameIntv : m_frameWait);
				if (m_frameIntvCounter == 0)
				{
					m_isStartCont = true;
					return true;
				}
				else
					return false;
			}
		}
		else
		{
			m_frameIntvCounter = 0;
			m_isStartCont = false;
			m_isDetected = false;
			return m_isDetected;
		}
	}
	
	private bool DetectDynamic()
	{
		if (m_isDetected && m_frameIntv > 0)
		{
			PostureDetector endPosDet = m_posts[PostureCount - 1];
			float lastScore = endPosDet.Score;
			//endPosDet.m_isCheckRise = false;
			if (endPosDet.Detect(false) && endPosDet.Score / lastScore > .5)
			{
				m_frameIntvCounter = (m_frameIntvCounter + 1) % (m_isStartCont ? m_frameIntv : m_frameWait);
				if (m_frameIntvCounter == 0)
				{
					m_isStartCont = true;
					return true;
				}
				else
					return false;
			}
			else
			{
				m_frameIntvCounter = 0;
				//endPosDet.m_isCheckRise = true;
				m_isStartCont = false;
			}
		}

		m_isPostDet[CurState] = m_posts[CurState].Detect(true);
		if (CurState == 1) m_isPostDet[0] = m_posts[0].Detect(false);
		m_isDetected = m_fsm.Update(m_isPostDet);
		return m_isDetected;
	}
	
}
