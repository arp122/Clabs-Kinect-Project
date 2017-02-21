using System;
using OpenNI;
using Joint = OpenNI.SkeletonJoint;

/// read the skeleton data from the (Kinect) sensor
/// derived from OpenNI's sample for Unity3D
public class SensorSkeletonReader
{
	/// @brief The user to player mapper (to figure out the user ID for each player).
    public NIPlayerManager m_playerManager;
	
	public int m_playerNumber = 0;
	
	public int m_jointCount;
	
	public JointData[] m_jointData;
	
	public float m_posDamping = .5f;
	
	
	public SensorSkeletonReader()
	{
		if (m_playerManager == null)
            m_playerManager = UnityEngine.Object.FindObjectOfType(typeof(NIPlayerManager)) as NIPlayerManager;
        if (m_playerManager == null)
            throw new System.Exception("Must have a player manager to control the skeleton!");
        m_jointCount = Enum.GetNames(typeof(Joint)).Length + 1; // Enum starts at 1
		
		m_jointData = new JointData[m_jointCount];
		ResetSkeletonData();
	}
	
	// reallocate the memories
	public void ResetSkeletonData()
	{
		for(int i = 0; i < m_jointCount; i++)
		{
			m_jointData[i] = JointData.NewObject();
		}
	}
	
	public bool IsSkeletonAvailable()
	{
		if (m_playerManager==null || m_playerManager.Valid == false)
            return false; // we can do nothing.

        NISelectedPlayer player = m_playerManager.GetPlayer(m_playerNumber);
        if (player == null || player.Valid == false || player.Tracking==false)
        {
            return false;
        }
		return true;
	}
	
	public bool UpdateSkeletonData()
	{
		if (!IsSkeletonAvailable())
            return false; // we can do nothing.

        NISelectedPlayer player = m_playerManager.GetPlayer(m_playerNumber);
        
		// we use the torso as root
        SkeletonJointTransformation skelTrans;
        if (player.GetSkeletonJoint(Joint.Torso, out skelTrans) == false)
        {
            // we don't have joint information so we simply return...
            return false;
        }

        // update each joint with data from OpenNI
        foreach (Joint joint in Enum.GetValues(typeof(Joint)))
        {
            SkeletonJointTransformation skelTransJoint;
            if(player.GetSkeletonJoint(joint,out skelTransJoint) == false)
                continue; // irrelevant joint
			m_jointData[(int)joint].FromRaw(skelTransJoint);
        }
		return true;
	}
	
}