
using System;
using UnityEngine;
using Joint = OpenNI.SkeletonJoint;

///@brief Component to control skeleton movement by the user's motion.
/// 
/// This class controls the skeleton avatar supplied to it in Unity3D. The user needs to drag & drop the
/// relevant joints (head, legs, shoulders etc.) and on update this will query OpenNI to get
/// the skeleton updated positions and rotate everything accordingly.
/// derived from OpenNI's sample for Unity3D: NISkeletonController.cs
public class SkeletonAvatarController : MonoBehaviour
{
    /// @brief The array of joints to control.
    /// 
    /// These transforms are what the user should drag & drop to in order to connect the skeleton 
    /// controller component to the skeleton it needs to control.
    /// @note not all transforms are currently supported in practice and therefore not all
    /// are necessary. Furthermore, if only some of the transforms are dragged, only those transforms will be
    /// moved. This means that in order for everything to work properly, all "rigged" joints need to be included
    /// (by "rigged" we mean those that will move all the important things in the model)
    public Transform[] m_jointTransforms;

    /// @brief If this is true the joint positions will be updated
    public bool m_updateJointPositions = false;
    /// @brief If this is true the entire game object will move
    public bool m_updateRootPosition = false;
    /// @brief If this is true the joint orientation will be updated
    public bool m_updateOrientation = true;
    /// @brief The speed in which the orientation can change
    public float m_rotationDampening = 15.0f;
    /// @brief Scale for the positions
    public float m_scale = 0.001f;
    /// @brief Speed for movement of the central position
    public float m_speed = 2.0f;

    /// Holds a line debugger which shows the lines connecting the joints. If null, nothing is
    /// drawn.
    public NISkeletonControllerLineDebugger m_linesDebugger;
    
	private bool m_isPausing = false;
	
	public bool Pause
	{
		set { m_isPausing = value; }
		get { return m_isPausing; }
	}
	
	private bool m_isUpdatingOnce = false;
	
	private bool m_isDebugLineActive = true;
	
	///  the initial rotations of the joints we move everything compared to this.
    protected Quaternion[] m_jointsInitialRotations;

    /// the current root position of the game object (movement is based on this).
    protected Vector3 m_rootPosition;

    /// the original (placed) root position of the game object.
    protected Vector3 m_originalRootPosition;

	public JointData[] m_jointData;
	
	public Vector3 m_centerOffset;
	
	private Quaternion m_originalRootRotation;
	private Quaternion[] m_originalJointRot;
	private Joint[] m_restoreJointIdx = new Joint[]{Joint.Head, Joint.Neck};
	
	
	// change the data source
	public void SetJointDataSource(JointData[] jointData)
	{
		m_jointData = jointData;
	}
	
	public void UpdateOnce()
	{
		m_isUpdatingOnce = true;
	}
	
	/// set the visibility of the debug lines (a skeleton connected by lines, beside the avatar)
	public void SetDebugLineActive(bool b)
	{
		m_isDebugLineActive = b;
		NISkeletonControllerLineRenderer lr = m_linesDebugger as NISkeletonControllerLineRenderer;
		if(lr != null)
			lr.SetVisible(b);
	}
	
	/// make the avatar return to initial position and rotation
	public void ReturnToInitRot()
	{
		transform.position = m_originalRootPosition - m_centerOffset*m_scale;
		transform.rotation = m_originalRootRotation;
		
		RotateToCalibrationPose();
		for(int i = 0; i < m_restoreJointIdx.Length; i++)
        {
			// sometimes the rotations of head and neck are weird
			m_jointTransforms[(int)m_restoreJointIdx[i]].localRotation = m_originalJointRot[i];
        }
	}
	
    /// Activates / deactivate the game object (skeleton and all sub objects)
    /// @param state on true sets to active, on false sets to inactive
    public void SetSkeletonActive(bool state)
    {
//        gameObject.SetActiveRecursively(state);
		gameObject.SetActive(state);
    }

    /// mono-behavior Initialization
    public void Start()
    {
        int jointCount = Enum.GetNames(typeof(Joint)).Length + 1; // Enum starts at 1
        m_jointsInitialRotations = new Quaternion[jointCount];
		m_originalRootRotation = transform.rotation; // yk
		m_originalJointRot = new Quaternion[m_restoreJointIdx.Length];
		for(int i = 0; i < m_restoreJointIdx.Length; i++)
        {
			// sometimes the rotations of head and neck are weird
			m_originalJointRot[i] = m_jointTransforms[(int)m_restoreJointIdx[i]].localRotation;
        }
		
        // save all initial rotations
        // NOTE: Assumes skeleton model is in "T" pose since all rotations are relative to that pose
        foreach (Joint j in Enum.GetValues(typeof(Joint)))
        {
            if((int)j>=m_jointTransforms.Length)
                continue; // if we increased the number of joints since the initialization.
            if (m_jointTransforms[(int)j])
            {
                // we will store the relative rotation of each joint from the game object rotation
                // we need this since we will be setting the joint's rotation (not localRotation) but we 
                // still want the rotations to be relative to our game object
                // Quaternion.Inverse(transform.rotation) gives us the rotation needed to offset the game object's rotation
                m_jointsInitialRotations[(int)j] = Quaternion.Inverse(transform.rotation) * m_jointTransforms[(int)j].rotation;
                if (m_linesDebugger != null)
                {
                    m_linesDebugger.InitJoint(j);
                }
            }
        }
        m_originalRootPosition = transform.localPosition;
        // start out in calibration pose
        RotateToCalibrationPose();
		m_centerOffset = new Vector3(0,-500,0);
    }


    /// mono-behavior update (called once per frame)
	public void Update () 
    {
		if(m_jointData == null)
            return; // we can do nothing.
		if(m_isPausing)
		{
			if(m_isUpdatingOnce) // update once
				m_isUpdatingOnce = false;
			else
				return;
		}
		
        UpdateRoot();
		
		foreach (Joint joint in Enum.GetValues(typeof(Joint)))
        {
			if(m_jointData[(int)joint].m_posConf < 0)
                continue; // irrelevant joint
            UpdateJoint(joint);
			
        }
	}
	 
	/// @brief updates the root position
    /// 
    /// This method updates the root position and if m_updateRootPosition is true, also move the entire transform
    /// @note we do not update if we do not have a high enough confidence!
    /// @param skelRoot the new central position
    /// @param centerOffset the offset we should use on the center (when moving the root). 
    /// This is usually the starting position (so the skeleton will not "jump" when doing the first update
    protected void UpdateRoot()
    {
		JointData jd = m_jointData[(int)Joint.Torso];
		if(jd.m_posConf < .5f)
            return; // we are not confident enough!
        m_rootPosition = jd.m_pos; // NIConvertCoordinates.ConvertPos(skelRoot.Position);
        m_rootPosition -= m_centerOffset;
        m_rootPosition *= m_scale * m_speed;
        m_rootPosition = transform.rotation * m_rootPosition;
        m_rootPosition += m_originalRootPosition; 
        if (m_updateRootPosition)
        {
            transform.position =  m_rootPosition;
        }
    }
	
	/// @brief updates a single joint
    /// 
    /// This method updates a single joint. The decision of what to update (orientation, position)
    /// depends on m_updateOrientation and m_updateJointPositions. Only joints with high confidence
    /// are updated. @note it is possible to update only position or only orientation even though both
    /// are expected if the confidence of one is low.
    /// @param centerOffset the new central position
    /// @param joint the joint we want to update
    /// @param skelTrans the new transformation of the joint
    protected void UpdateJoint(Joint joint)
    {
        // make sure something is hooked up to this joint
        if ((int)joint >= m_jointTransforms.Length || !m_jointTransforms[(int)joint])
        {
            return;
        }
		
		JointData jd = m_jointData[(int)joint];
        Quaternion rot = CalcRotationForJoint(joint);
		Vector3 pos0 = (jd.m_pos - m_centerOffset) * m_scale;
        // if we have debug lines to draw we need to collect the data.
        if (m_isDebugLineActive & m_linesDebugger != null)
        {
            Vector3 pos = pos0*1.2f + // magnify the scale of the debugger by 1.2 times
				transform.position +
				new Vector3(1,0,0); // add a offset, from the avatar
            float posConf = jd.m_posConf;
            float rotConf = jd.m_orientConf;
            m_linesDebugger.UpdateJointInfoForJoint(joint, pos, posConf, rot, rotConf);

        }
        
        // modify orientation (if needed and confidence is high enough)
		// the confidence of the hands are always 0, maybe also ankles
        if (m_updateOrientation && jd.m_orientConf >= 0.5 && joint != Joint.LeftHand && joint != Joint.RightHand)
        {
            m_jointTransforms[(int)joint].rotation = rot;
        }

        // modify position (if needed, and confidence is high enough)
        if (m_updateJointPositions && jd.m_posConf >= 0.5)
        {
            m_jointTransforms[(int)joint].localPosition = pos0; // CalcJointPosition(jd);
        }
    }
	
	/// @brief Utility method to calculate the rotation of a joint
    /// 
    /// This method receives joint information and calculates the rotation of the joint in Unity
    /// coordinate system.
    /// @param centerOffset the new central position
    /// @param joint the joint we want to calculate the rotation for
    /// @param skelTrans the new transformation of the joint
    /// @return the rotation of the joint in Unity coordinate system
    protected Quaternion CalcRotationForJoint(Joint joint)
    {
        // In order to convert the skeleton's orientation to Unity orientation we will
        // use the Quaternion.LookRotation method to create the relevant rotation Quaternion. 
        // for Quaternion.LookRotation to work it needs a "forward" vector and an "upward" vector.
        // These are generally the "Z" and "Y" axes respectively in the sensor's coordinate
        // system. The orientation received from the skeleton holds these values in their 
        // appropriate members.

         // Get the forward axis from "z".
		JointData jd = m_jointData[(int)joint];
		Vector3 worldForward = jd.m_orient[2];
        worldForward *= -1.0f; // because the Unity "forward" axis is opposite to the world's "z" axis.
        if (worldForward.magnitude == 0)
            return Quaternion.identity; // we don't have a good point to work with.
        // Get the upward axis from "Y".
		Vector3 worldUpwards = jd.m_orient[1];
        if (worldUpwards.magnitude == 0)
            return Quaternion.identity; // we don't have a good point to work with.
        Quaternion jointRotation = Quaternion.LookRotation(worldForward, worldUpwards);

        Quaternion newRotation = transform.rotation * jointRotation * m_jointsInitialRotations[(int)joint];

        // we try to limit the speed of the change.
		float t = Time.deltaTime * m_rotationDampening;
        return Quaternion.Slerp(m_jointTransforms[(int)joint].rotation, newRotation, t);
    }

    /// This rotates the skeleton to the requested calibration position.
    /// @note it assumes the initial position is a "T" position and orients accordingly (returning
    /// everything to its base and then rotating the arms).
    public void RotateToCalibrationPose()
    {
        foreach (Joint j in Enum.GetValues(typeof(Joint)))
        {
            if ((int)j<m_jointTransforms.Length && m_jointTransforms[(int)j]!=null)
            {
                m_jointTransforms[(int)j].rotation = transform.rotation * m_jointsInitialRotations[(int)j];
            }
        }
        // calibration pose is skeleton base pose ("T") with both elbows bent in 90 degrees
        Transform elbow=m_jointTransforms[(int)Joint.RightElbow];
        if (elbow != null)
            elbow.rotation = transform.rotation * Quaternion.Euler(0, -90, 90) * m_jointsInitialRotations[(int)Joint.RightElbow];
        elbow = m_jointTransforms[(int)Joint.LeftElbow];
        if (elbow != null)
            elbow.rotation = transform.rotation * Quaternion.Euler(0, 90, -90) * m_jointsInitialRotations[(int)Joint.LeftElbow];
        if (m_updateJointPositions)
        {
            // we want to position the skeleton in a calibration pose. We will therefore position select
            // joints
            UpdateJointPosition(Joint.Torso,0,0,0);
            UpdateJointPosition(Joint.Head, 0, 450, 0);
            UpdateJointPosition(Joint.LeftShoulder, -150, 250, 0);
            UpdateJointPosition(Joint.RightShoulder, 150, 250, 0);
            UpdateJointPosition(Joint.LeftElbow, -450, 250, 0);
            UpdateJointPosition(Joint.RightElbow, 450, 250, 0);
            UpdateJointPosition(Joint.LeftHand, -450, 450, 0);
            UpdateJointPosition(Joint.RightHand, 450, 450, 0);
            UpdateJointPosition(Joint.LeftHip, -100, -250, 0);
            UpdateJointPosition(Joint.RightHip, 100, -250, 0);
            UpdateJointPosition(Joint.LeftKnee, -100, -700, 0); 
            UpdateJointPosition(Joint.RightKnee, 100, -700, 0); 
            UpdateJointPosition(Joint.LeftFoot, -100, -1150, 0);
            UpdateJointPosition(Joint.RightFoot, 100, -1150, 0);

        }
    }
	
    /// @brief a utility method to update joint position 
    /// 
    /// This utility method receives a joint and unscaled position (x,y,z) and moves the joint there.
    /// it makes sure the joint has been attached and that scale is applied.
    /// @param joint The joint to update (the method makes sure it is legal)
    /// @param xPos The unscaled position along the x axis (scale will be applied)
    /// @param yPos The unscaled position along the y axis (scale will be applied)
    /// @param zPos The unscaled position along the z axis (scale will be applied)
    protected void UpdateJointPosition(Joint joint, float xPos, float yPos, float zPos)
    {
        if(((int)joint)>=m_jointTransforms.Length || m_jointTransforms[(int)joint] == null)
            return; // an illegal joint
        Vector3 tmpPos = Vector3.zero;
        tmpPos.x = xPos;
        tmpPos.y = yPos;
        tmpPos.z = zPos;
        tmpPos *= m_scale;
        m_jointTransforms[(int)joint].localPosition = tmpPos;
    }

}
