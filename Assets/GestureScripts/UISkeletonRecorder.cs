using System;
using System.IO;
using UnityEngine;

/// draws and handles the skeleton recorder interface
public class UISkeletonRecorder
{
	/// handles the recorded data
	public SkeletonRecorder m_skRecorder;
	
	private bool m_isRecording = false;
	
	private string m_showStr;
	private string m_showStrHeader = "";
	
	private string m_inputStr;
	
	private GestureManager m_gestManager;
	

	public UISkeletonRecorder (JointData[] jointData, GestureManager gm)
	{
		m_skRecorder = new SkeletonRecorder (null, jointData);
		m_gestManager = gm;
	}
		
	public void Init()
	{
		m_inputStr = ConstDef.defaultFileName;
		m_showStr = "Enter the file name you want to open (" + ConstDef.skelRootPath + "*" + ConstDef.skelFileSuffix + ")";
	}
	
	public void OnGUI ()
	{
		GUILayout.BeginVertical ();
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button (m_isRecording ? "Stop recording" : "Start recording"))
			OnRecordButton();
		m_inputStr = GUILayout.TextField(m_inputStr, GUILayout.Width(100));
		if (GUILayout.Button ("Reimport initial gesture templates"))
			OnLoadButton();
		
		GUILayout.EndHorizontal ();
		GUILayout.Label(m_showStr);
		GUILayout.EndVertical ();
	}
	
	private void OnRecordButton()
	{
		if(!m_isRecording)
		{
			if(! m_gestManager.m_skReader.IsSkeletonAvailable())
				m_showStr = "Please record after Kinect detects your skeleton";
			else
			{
				m_showStrHeader = "Recording...";
				m_showStr = m_showStrHeader;
				m_skRecorder.Reset();
				m_isRecording = true;
			}
		}
		else
		{
			if(!Directory.Exists(ConstDef.skelRootPath))
				Directory.CreateDirectory(ConstDef.skelRootPath);
			string filename = m_inputStr;
			if(! filename.EndsWith(ConstDef.skelFileSuffix))
				filename += ConstDef.skelFileSuffix;
			if(File.Exists(ConstDef.skelRootPath + filename))
				filename = "skeleton data " + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss") + ConstDef.skelFileSuffix;
			
			try
			{
				using(StreamWriter writer = new StreamWriter(ConstDef.skelRootPath + filename))
				{
					m_skRecorder.SaveFile(writer);
				}
				m_showStr = "Skeleton data have been saved to "+filename;
			}
			catch(Exception e)
			{
				Debug.LogException(e);
				m_showStr = filename+": error occurred when saving";
			}
				
			m_isRecording = false;
		}
		
		ConstDef.defaultFileName = m_inputStr;
	}

	public void OnLoadButton ()
	{
		m_gestManager.Pause = true;
		int loaded = m_gestManager.LoadGestureTemplates();
		m_showStr = string.Format("{0} gesture templates loaded",loaded);
		m_gestManager.Pause = false;
	}
	
	public void Update()
	{
		if(m_isRecording)
		{
			m_skRecorder.RecordOneFrame();
			m_showStr = m_showStrHeader + m_skRecorder.FrameCount + "frames / " + 
				string.Format("{0:F1}s",m_skRecorder.TimeElapsed);
		}
	}
}

