using System.Collections.Generic;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// a member of UITemplateTrainer. control the training process
public class TemplateTrainer
{
	private SkeletonRecorder m_skRecorder;

	private JointFeature m_postFeature;

	public Joint[] m_jointIdx = ConstDef.defaultDetectJointIdx;

	/// true: use relative algorithm; false: absolute; 
	/// null: auto select algorithm for each joint according to the scatter of the training samples
	public bool? m_isUseRelativeFt = null;

	public int m_tagCount;

	/// the count of joints we need to check in the template, not the count of all the joints, 
	/// neither the recorded joints (m_skRecorder.JointCount)
	public int JointCount { get { return m_jointIdx.Length; } }


	public TemplateTrainer(SkeletonRecorder skRecorder, JointFeature pf)
	{
		m_skRecorder = skRecorder;
		m_postFeature = pf;
	}

	public GestureDetector Train(string name)
	{
		GestureDetector gd;

		// actually, the absolute feature seems a little better than the relative feature or automatically selected feature.
		// so using the absolute generally is mostly enough.
		if (m_isUseRelativeFt != null)
		{
			gd = InitTrain(name, m_isUseRelativeFt==true);
			/*float[,] stdArray = */DoTrain(ref gd);
		}
		else // auto select absolute or relative feature according to the std
		{
			gd = InitTrain(name, false);
			float[,] stdArray = DoTrain(ref gd);
			GestureDetector gdr = InitTrain(name, true);
			float[,] stdArrayr = DoTrain(ref gdr);
			for (int i = 1; i < m_tagCount; i++)
			{
				for (int j = 0; j < JointCount; j++)
				{
					if (stdArray[i,j] > stdArrayr[i,j])
					{
						gd[i][j] = gdr[i][j];
					}
				}
			}
		}

		return gd;
	}

	private GestureDetector InitTrain(string name, bool isUseRel)
	{
		m_tagCount = Mathf.Max(m_skRecorder.m_tagList.ToArray());
		GestureDetector gd = new GestureDetector();
		gd.m_name = name;
		gd.m_jointIdx = (Joint[])m_jointIdx.Clone();
		gd.m_thresMulPerPost = new float[m_tagCount];
		gd.m_thresMulPerJoint = new float[JointCount];

		PostureDetector[] pd = new PostureDetector[m_tagCount];
		gd.m_posts = pd;
		for (int i = 0; i < m_tagCount; i++)
		{
			gd.m_thresMulPerPost[i] = 1;
			pd[i] = new PostureDetector();
			pd[i].m_name = name + '_' + (i + 1);

			JointMatcher[] jm = new JointMatcher[JointCount];
			pd[i].m_jointMatcher = jm;
			for (int j = 0; j < JointCount; j++)
			{
				if (i == 0 || !isUseRel)
					jm[j] = new JointMatcher();
				else
				{
					jm[j] = new JointMatcherR();
					((JointMatcherR)jm[j]).SetLastPostJoint(gd, i, j);
				}
				jm[j].m_jointIdx = m_jointIdx[j];
				gd.m_thresMulPerJoint[j] = 1;
			}
		}

		return gd;
	}

	private float[,] DoTrain(ref GestureDetector gd)
	{
		/* arrange the key frame data in m_skRecorder.m_dataList */
		m_postFeature.Reset();
		m_postFeature.RegisterJoints(m_jointIdx); // set the feature type and joint indices to calculate features
		for (int i = 0; i < m_skRecorder.FrameCount; i++)
		{
			int tag = m_skRecorder.m_tagList[i];
			if (tag == 0)
				continue;
			CopyCurFrameJointDataRef(i);
			m_postFeature.UpdateFeatures(); // calculate the features
			for (int j = 0; j < JointCount; j++)
			{
				gd.m_posts[tag - 1][j].AddTrainData(m_postFeature.m_jointVec[(int)m_jointIdx[j]]);
			}
		}

		/* train the posture templates */
		float[,] stdArray = new float[m_tagCount, JointCount];
		for (int i = 0; i < m_tagCount; i++)
		{
			for (int j = 0; j < JointCount; j++)
			{
				stdArray[i, j] = gd.m_posts[i][j].Train();
			}
		}
		// train time interval coefficients
		TrainTimeIntv(gd);

		return stdArray;
	}

	/*
	// output the joint feature data to a file for external analysis
	private void Output()
	{
		using (StreamWriter writer = new StreamWriter("jointVecs.txt", true))
		{
			int[] pointers = new int[m_tagCount];
			for (int i = 0; i < m_skRecorder.FrameCount; i++)
			{
				int tag = m_skRecorder.m_tagList[i];
				Vector3[] vecs = m_vecList[tag - 1][pointers[tag - 1]++];
				for (int j = 0; j < JointCount; j++)
				{
					writer.Write(string.Format("{0:F2}, {1:F2}, {2:F2}, ",
						vecs[j].x, vecs[j].y, vecs[j].z));
				}
				writer.Write(string.Format("{0}, {1}", tag, m_skRecorder.m_fileIdxList[i]));
				writer.WriteLine();
			}
			writer.WriteLine();
		}
	}
	*/
	private void TrainTimeIntv(GestureDetector gd)
	{
		List<float>[] timeList = new List<float>[m_tagCount - 1];
		for (int i = 0; i < m_tagCount - 1; i++) timeList[i] = new List<float>();
		
		/* arrange the key frame delta time in m_skRecorder.m_timeList */
		int lastKeyFrameIdx = -1, lastKeyFrameTag = -1;
		for (int i = 0; i < m_skRecorder.FrameCount; i++)
		{
			int tag = m_skRecorder.m_tagList[i];
			if (tag == 0)
				continue;

			if (tag == lastKeyFrameTag + 1)
			{
				timeList[tag - 2].Add(m_skRecorder.m_timeList[i] - m_skRecorder.m_timeList[lastKeyFrameIdx]);
			}
			lastKeyFrameTag = tag;
			lastKeyFrameIdx = i;
		}

		/* train the delta time template */
		float[] intv = new float[m_tagCount-1];
		float[] intvThrs = new float[m_tagCount-1];
		for (int t = 0; t < m_tagCount - 1; t++)
		{
			float avg, std;
			Stat.AvgStdRobust(timeList[t].ToArray(), out avg, out std);
			intv[t] = avg;
			intvThrs[t] = ConstDef.timeThresMul * std;
		}

		gd.m_intervals = intv;
		gd.m_intvThres = intvThrs;
	}

	// only the reference is copied from the recorded joint data to the joint array to compute the features!
	private void CopyCurFrameJointDataRef(int frameIdx)
	{
		JointData[] jda = m_skRecorder.m_dataList[frameIdx];
		for (int i = 0; i < m_skRecorder.JointCount; i++)
		{
			Joint j = m_skRecorder.m_jointIdx[i];
			m_postFeature.m_jointData[(int)j] = jda[i];
		}
	}

}
