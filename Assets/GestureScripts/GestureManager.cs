using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// manage the gesture detectors
public class GestureManager : MonoBehaviour {
	
	public SensorSkeletonReader m_skReader;
	
	public List<GestureDetector> m_gestures;
	
	public int GestureCount { get { return m_gestures.Count; } }
	
	/// true if the detection is pausing
	public bool Pause { get; set; }

	/// true if the detection is using real time data from m_skReader
	public bool UseRealtimeData { get; set; }
	
	/// a feature calculator
	public JointFeature m_postFeature;
	
	/// gesture event handler
	public event EventHandler<GestureEventArgs> m_gestureDetected;
	
	public GestureDetector this[int gestNo]
	{
		get
		{
			return m_gestures[gestNo];
		}
	}
	
	
	void Awake()
	{
		m_skReader = new SensorSkeletonReader(); // in case other scripts want to find m_skReader in Start()
	}
	
	/// Use this for initialization
	public void Start ()
	{
		Pause = false;
		UseRealtimeData = true;
		m_postFeature = new JointFeature(m_skReader.m_jointData);
		int loaded = LoadGestureTemplates();
		Debug.Log(string.Format("{0} gesture templates loaded.",loaded));
	}
	
	public int LoadGestureTemplates()
	{
		m_gestures = new List<GestureDetector>();
        using (StreamReader reader = new StreamReader(ConstDef.tmplRootPath + ConstDef.gestureTemplateListName))
        {
			while(reader.Peek() >= 0)
			{
            	string filename = reader.ReadLine();
				if(filename[0] == '#') continue;
				if(! filename.EndsWith(ConstDef.tmplFileSuffix))
					filename += ConstDef.tmplFileSuffix;
				try
				{
					GestureDetector gd = GestureDetector.LoadFromFile(filename);
					gd.Init(m_postFeature);
					gd.m_frameIntv = 0;
					m_gestures.Add(gd);
					Debug.Log("Gesture " + gd.m_name + " in " + filename + " loaded.");
				} 
				catch (Exception e) 
				{
					Debug.LogException(e);
				}
			}
        }

		return GestureCount;
	}
	
	/// the main work flow. called once per frame
	void Update ()
	{
		if(Pause) return;
		
		if(UseRealtimeData && !m_skReader.UpdateSkeletonData()) return;
		
		m_postFeature.UpdateFeatures();
		
		foreach(GestureDetector gd in m_gestures)
		{
			if (!gd.CheckPositionConfidence())
			{
				continue; // if data not confident, won't do detection
			}
			if(gd.Detect())
			{
				if(m_gestureDetected != null)
				{
					m_gestureDetected(this, new GestureEventArgs(gd.m_name, gd.PostureCount==1)); // trigger the event
				}
			}
		}
	}
}

/// argument type for gesture events
public class GestureEventArgs : EventArgs
{
	public GestureEventArgs( string gestureName, bool isPosture )
	{
		GestureName = gestureName;
		IsPosture = isPosture;
	}

	public string GestureName { get; set; }
	
	/// if the gesture is static (only have one key posture)
	public bool IsPosture { get; set; }
	
}