using System;
using System.IO;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

/// draws and handles the skeleton player interface
public class UISkeletonPlayer
{
	private SkeletonRecorder m_skRecorder;

	/// the same data array in SkeletonAvatarController and SensorSkeletonReader
	private JointData[] m_avJointData;
	
	public bool m_isPlaying = false;
	
	private string m_showStr;
	
	private string m_inputStr;
	
	public int m_curFrame = 0;
	
	private float m_startTime = 0;
	
	private float m_pauseTime = 0;
	
	private float m_playTimeOffset = 0;
	
	
	public UISkeletonPlayer (SkeletonRecorder skRecorder)
	{
		m_skRecorder = skRecorder;
	}
	
	public void Init()
	{
		m_inputStr = ConstDef.defaultFileName;
		m_playTimeOffset = 0;
		m_avJointData = m_skRecorder.m_jointData;
		m_showStr = m_skRecorder.FrameCount == 0 ? "Skeleton file not opened" : "Skeleton data already exist";
		m_showStr += ". Enter the file name you want to open (" + ConstDef.skelRootPath + "*" + ConstDef.skelFileSuffix + ")";
		m_curFrame = 0;
		CopyCurFrameJointDataRef(m_curFrame);
	}
	
	public void OnGUI()
	{
		GUILayout.BeginVertical ();
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button("Open skeleton file"))
			OnOpenButton();
		m_inputStr = GUILayout.TextField(m_inputStr, GUILayout.Width(100));
		GUI.enabled = m_skRecorder.FrameCount > 0;
		if (GUILayout.Button (m_isPlaying ? "Pause" : "Play"))
			OnPlayButton();
		if (GUILayout.Button ("<<"))
			OnRewindButton();
		GUI.enabled = true;
		GUILayout.EndHorizontal ();
		GUILayout.Label(m_showStr);
		GUILayout.EndVertical ();
	}
	
	public void Update()
	{
		if(m_isPlaying)
		{
			float elapseTime = Time.time - m_startTime;
			if(elapseTime < m_skRecorder.m_timeList[m_curFrame] + m_playTimeOffset) // in case the playing is too fast
				return;
			// in case the timeList don't increase monotonously, for example, data are from multiple files
			if(m_curFrame < m_skRecorder.FrameCount-1 &&
				m_skRecorder.m_timeList[m_curFrame+1] <= m_skRecorder.m_timeList[m_curFrame])
				m_playTimeOffset += m_skRecorder.m_timeList[m_curFrame] - m_skRecorder.m_timeList[m_curFrame+1];
			
//			Debug.Log(elapseTime);
			if(m_curFrame < m_skRecorder.FrameCount - 1)
			{
				m_curFrame++;
				CopyCurFrameJointDataRef(m_curFrame);
			}
			else
			{
				m_isPlaying = false;
				m_curFrame = 0;
			}
		}
	}
	
	private void OnOpenButton()
	{
		string filename = m_inputStr;
		if(filename == "")
		{
			m_showStr = "Please enter the file name";
			return;
		}
		
		if(! filename.EndsWith(ConstDef.skelFileSuffix))
			filename += ConstDef.skelFileSuffix;
		try
		{
			using(StreamReader reader = new StreamReader(ConstDef.skelRootPath + filename))
			{
				m_skRecorder.LoadFileNew(reader);
			}
			m_showStr = filename + ": imported";
			m_curFrame = 0;
		}
		catch(Exception e)
		{
			Debug.LogException(e);
			m_showStr = filename + ": import error";
		}
			
		ConstDef.defaultFileName = m_inputStr;
	}
	
	private void OnPlayButton()
	{
		m_isPlaying = ! m_isPlaying;
		if(!m_isPlaying)
			m_pauseTime = Time.time;
		else
		{
			if(m_curFrame == 0)
			{
				m_playTimeOffset = 0;
				m_startTime = Time.time;
			}
			else
				m_startTime += Time.time-m_pauseTime;
		}
	}
	
	// only the reference is copied from the recorded joint data to the joint array to control the avatar!
	private void CopyCurFrameJointDataRef(int frameIdx)
	{
		if(frameIdx > m_skRecorder.FrameCount-1)
			return;
		JointData[] jda = m_skRecorder.m_dataList[frameIdx];
		for(int i = 0; i < m_skRecorder.JointCount; i++)
		{
			Joint j = m_skRecorder.m_jointIdx[i];
			m_avJointData[(int)j] = jda[i];
		}
	}
	
	public void OnRewindButton ()
	{
		m_curFrame = 0;
		m_playTimeOffset = 0;
		m_startTime = Time.time;
		CopyCurFrameJointDataRef(m_curFrame);
	}
	
}
