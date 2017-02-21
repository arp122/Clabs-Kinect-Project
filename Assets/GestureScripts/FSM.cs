using System;
using UnityEngine;

/// a simple finite state machine to check if the postures included by a gesture are successively detected
public class FSM
{
	public int m_stateCount = 0;
	
	/// current state
	public int m_state = 0;
	
	public float m_stateStartTime = 0;

	/// the time that the state has lasted
	public float m_stateContTime = 0;

	/// arrays with length m_stateCount-1. the time interval between state i+1 and i+2 should be between
	/// intervals[i] - intvThres[i] * m_thresMul and intervals[i] + intvThres[i] * m_thresMul
	public float[] m_intervals;
	public float[] m_intvThres;

	/// multiplier for time interval thresholds
	public float m_thresMul = 1;


	public FSM(int stateCount, float[] intervals, float[] intvThres)
	{
		if(stateCount != intervals.Length+1)
			throw new Exception("stateCount should be intervals.Length+1");
		m_stateCount = stateCount;
		m_intervals = (float[])intervals.Clone();
		m_intvThres = (float[])intvThres.Clone();
	}
	
	/// update the state of the FSM
	/// @param triggers an array with length m_stateCount. triggers[i]==true indicates that the i+1'th (starting from 1)
	/// posture has been detected, and the state should updated to i+1, if not too fast.
	/// @return true if final state have been reached
	public bool Update(bool[] triggers)
	{
		m_stateContTime = Time.time - m_stateStartTime;
		if(m_state == 0)
		{
			m_stateStartTime = Time.time;
			m_stateContTime = 0;
			if(triggers[0]) NextState();
			else return false;
		}
		else if(m_state == 1)
		{
			if (triggers[1] && NotTooFast()) 
			{
				NextState();
			}
			else if (triggers[0]) // in the 1st state, one can stay as long as triggers[0]==true, until the next state is triggered
			{
				m_stateStartTime = Time.time;
			}
			else CheckTimeOut();
		}
		else if (m_state != m_stateCount)
		{
			if (triggers[m_state] && NotTooFast()) NextState();
			else CheckTimeOut();
		}

		if (m_state == m_stateCount) // reach final state
		{
			Reset();
			return true;
		}
		return false;
	}
	
	private void NextState()
	{
		m_state++;
		m_stateStartTime = Time.time;
	}
	
	private void CheckTimeOut()
	{
		if(m_stateContTime > m_intervals[m_state-1] + m_intvThres[m_state-1] * m_thresMul) // time out
		{
			Reset();
		}
	}
	
	/// @return true if the time interval is not too short
	private bool NotTooFast()
	{
		return m_stateContTime > m_intervals[m_state-1] - m_intvThres[m_state-1] * m_thresMul;
	}
	
	private void Reset()
	{
		m_state = 0;
		m_stateStartTime = 0;
		m_stateContTime = 0;
	}

}
