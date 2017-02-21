using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// this is a very important class. It is used throughout the training process to record skeleton data.
public class SkeletonRecorder
{
	/// the number of data we record for each joint. the 9 data are
	/// position:(x,y,z); forward direction:(z1,z2,z3); upward direction:(y1,y2,y3)
	private int m_dataPerJoint = 9;

	/// the joints we record
	public Joint[] m_jointIdx = ConstDef.defaultRecordJointIdx;

	/// the recorded data
	public List<JointData[]> m_dataList;

	/// the time of record
	public List<float> m_timeList;
	
	/// the tag of each recorded frame (indicating key frames)
	public List<int> m_tagList;

	/// the index of the file when loading data from skeleton files
	private List<int> m_fileIdxList;

	private int m_curFileIdx;

	/// the minimum time interval in seconds between two recorded frames
	private float m_timeInterval = ConstDef.minRecordIntv;

	/// the time the record started
	private float m_initTime = 0;

	/// the time last frame was recorded
	private float m_lastTime = 0;

	public JointData[] m_jointData;

	/// the time the recorded has lasted
	public float TimeElapsed { get { return m_lastTime-m_initTime; } }

	/// total recorded frames
	public int FrameCount { get { return m_timeList.Count; } }

	/// total recorded frames with tag != 0
	public int TaggedFrameCount
	{
		get { return (from t in m_tagList
						where t != 0
						select t).Count(); } }
	
	public int JointCount { get { return m_jointIdx.Length; } }

	
	public SkeletonRecorder (Joint[] jointIdx, JointData[] jointData)
	{
		if(jointIdx != null)
			m_jointIdx = (Joint[])jointIdx.Clone();
		m_jointData = jointData;
		Reset();
	}

	public void Reset()
	{
		m_dataList = new List<JointData[]> ();
		m_timeList = new List<float> ();
		m_tagList = new List<int> ();
		m_fileIdxList = new List<int>();
		m_curFileIdx = 0;
	}

	public void RecordOneFrame ()
	{
		float curTime = Time.time;
		if(m_dataList.Count == 0)
			m_initTime = Time.time;
		if(curTime-m_lastTime > m_timeInterval)
		{
			m_lastTime = curTime;
			int i = 0;
			JointData[] jda = new JointData[JointCount];
			foreach (Joint j in m_jointIdx)
				jda[i++] = new JointData(m_jointData[(int)j]);
			m_dataList.Add(jda);
			m_timeList.Add(TimeElapsed);
			m_tagList.Add(0);
		}
	}
	
	public int SaveFile(StreamWriter writer)
	{
		writer.WriteLine("# Skeleton data file");
		writer.WriteLine("# Lines 5~8: frame count; joint count; joint names; data count per joint");
		writer.WriteLine("# Line 10: frame index. Line 11: time stamp. Line 12: tag. Line 13: 1st joint, position:(x,y,z); " +
			"forward direction:(z1,z2,z3); upward direction:(y1,y2,y3). Line 14: 2nd joint: ...");
		writer.WriteLine();
		writer.WriteLine(FrameCount);
		writer.WriteLine(JointCount);
		for(int i = 0; i < JointCount; i++)
		{
			writer.Write(m_jointIdx[i].ToString());
			if(i != JointCount-1)
				writer.Write(',');
		}
		writer.WriteLine();
		writer.WriteLine(m_dataPerJoint);
		writer.WriteLine();
		
		for(int i = 0; i < FrameCount; i++)
		{
			string str = string.Format("{0}\r\n{1:F0}\r\n{2}\r\n", i + 1, m_timeList[i] * 1000, m_tagList[i]);
			for(int j = 0; j < JointCount; j++)
			{
				float[] processedData = JD2Array(m_dataList[i][j]);
				for(int d = 0; d < m_dataPerJoint; d++)
				{
					str += (int)processedData[d];
					if(d != m_dataPerJoint-1)
						str += '\t';
				}
				str += "\r\n";
			}
			writer.WriteLine(str);
		}
		return FrameCount;
	}
	
	/// write data in jd to an array to return
	/// data in the array: position:(x,y,z); forward direction:(z1,z2,z3); upward direction:(y1,y2,y3)
	private float[] JD2Array(JointData jd)
	{
		return new float[9]{
			jd.m_pos.x, jd.m_pos.y, jd.m_pos.z,
			jd.m_orient[1].x*100, jd.m_orient[1].y*100, jd.m_orient[1].z*100,
			jd.m_orient[2].x*100, jd.m_orient[2].y*100, jd.m_orient[2].z*100};
	}

	/// load a file to replace data before
	public int LoadFileNew(StreamReader reader)
	{
		int frameCount, jointCount;
		ReadFileHeader(reader, out frameCount, out jointCount, out m_jointIdx, out m_dataPerJoint);
		m_dataList = new List<JointData[]>(frameCount);
		m_timeList = new List<float>(frameCount);
		m_tagList = new List<int>(frameCount);
		m_fileIdxList = new List<int>(frameCount);
		m_curFileIdx = 1;
		ReadFrames(reader, frameCount, jointCount);
		return frameCount;
	}

	/// load a file to append to the data before
	public int LoadFileAppend(StreamReader reader)
	{
		if(FrameCount == 0)
		{
			return LoadFileNew(reader);
		}
		
		int frameCount, jointCount, dataPerJoint = 0;
		Joint[] jointIdx;
		ReadFileHeader(reader, out frameCount, out jointCount, out jointIdx, out dataPerJoint);
		if( jointCount != JointCount || dataPerJoint != m_dataPerJoint)
			throw new Exception("appending skeleton data with different format!");
		for(int i = 0; i < jointCount; i++)
			if(jointIdx[i] != m_jointIdx[i])
			throw new Exception("appending skeleton data with different format!");
		
		m_dataList.Capacity += frameCount;
		m_timeList.Capacity += frameCount;
		m_tagList.Capacity += frameCount;
		m_fileIdxList.Capacity += frameCount;
		m_curFileIdx ++;
		ReadFrames(reader, frameCount, jointCount);
		return frameCount;
	}

	/// called by LoadFileNew and LoadFileAppend
	private void ReadFileHeader(StreamReader reader, 
		out int frameCount, out int jointCount, out Joint[] jointIdx, out int dataPerJoint)
	{
		string str;
		str = reader.ReadLine();
		str = reader.ReadLine();
		str = reader.ReadLine();
		str = reader.ReadLine();
		str = reader.ReadLine();
		frameCount = Convert.ToInt32(str);
		
		str = reader.ReadLine();
		jointCount = Convert.ToInt32(str);
		jointIdx = new Joint[jointCount];
		
		str = reader.ReadLine();
		string[] d = str.Split(',');
		for(int i = 0; i < jointCount; i++)
		{
			jointIdx[i] = (Joint)Enum.Parse(typeof(Joint),d[i]);
		}
		
		str = reader.ReadLine(); // dataPerJoint
		dataPerJoint = Convert.ToInt32(str);
	}

	/// called by LoadFileNew and LoadFileAppend
	private void ReadFrames(StreamReader reader, int frameCount, int jointCount)
	{
		int[] num = new int[m_dataPerJoint];
		string str;
		string[] d;
		
		for(int i = 0; i < frameCount; i++)
		{
			str = reader.ReadLine(); // \n
			str = reader.ReadLine(); // frame index
			str = reader.ReadLine();
			m_timeList.Add(Convert.ToInt32(str)/1000f);
			str = reader.ReadLine();
			m_tagList.Add(Convert.ToInt32(str));
			m_fileIdxList.Add(m_curFileIdx);
			JointData[] jda = new JointData[jointCount];
			
			for(int j = 0; j < jointCount; j++)
			{
				str = reader.ReadLine();
				d = str.Split('\t');
				for(int k = 0; k < m_dataPerJoint; k++)
				{
					num[k] = Convert.ToInt32(d[k]);
				}
				jda[j] = Array2JD(num);
			}
			m_dataList.Add(jda);
		}
	}
	
	/// inverse process of JD2Array
	private JointData Array2JD(int[] num)
	{
		JointData jd = JointData.NewObject();
		jd.m_pos.x = num[0]; jd.m_pos.y = num[1]; jd.m_pos.z = num[2];
		jd.m_posConf = 1;
		for(int i = 1; i < 3; i++)
		{
			jd.m_orient[i].x = num[i*3]/100f;
			jd.m_orient[i].y = num[i*3+1]/100f;
			jd.m_orient[i].z = num[i*3+2]/100f;
		}
		jd.m_orientConf = 1;
		return jd;
	}
	
	public void RemoveUntaggedFrames ()
	{
		List<JointData[]> newDataList = new List<JointData[]>();
		List<float> newTimeList = new List<float>();
		List<int> newTagList = new List<int>();
		
		for(int i = 0; i < FrameCount; i++)
		{
			if(m_tagList[i] != 0)
			{
				newDataList.Add(m_dataList[i]);
				newTimeList.Add(m_timeList[i]);
				newTagList.Add(m_tagList[i]);
			}
		}
		m_dataList = newDataList;
		m_timeList = newTimeList;
		m_tagList = newTagList;
	}
	
	public int SaveFile_onlyTaggedFrames(StreamWriter writer)
	{
		List<JointData[]> oldDataList = m_dataList;
		List<float> oldTimeList = m_timeList;
		List<int> oldTagList = m_tagList;
		RemoveUntaggedFrames();
		int ret = FrameCount;
		if(ret > 0)
			SaveFile(writer);
		m_dataList = oldDataList;
		m_timeList = oldTimeList;
		m_tagList = oldTagList;
		return ret;
	}
	
}
