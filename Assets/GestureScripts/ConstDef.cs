using Joint = OpenNI.SkeletonJoint;

/// some constant definitions
public static class ConstDef
{
	/// root path to store gesture template list and templates
	public const string tmplRootPath = @"gesture data\";

	/// root path to store skeleton data and training config list
	public const string skelRootPath = @"skeleton data\";
	

	/// gestures in this list are initially loaded
	public const string gestureTemplateListName = "gesture template names.txt";

	/// this file contains the training configurations
	public const string trainConfigListName = "train config.txt";


	/// file suffix for skeleton data file
	public const string skelFileSuffix = ".skeleton";

	/// file suffix for key frame file
	///
	/// the same with skelFileSuffix, expect the key frame file overwrite the skeleton file
	/// the only difference between skeleton file and key frame file is the latter have some frames with tags != 0
	public const string KFFileSuffix = ".skeleton";
	
	/// file suffix for gesture templates
	public const string tmplFileSuffix = ".xml";
	
	public const string defaultGestName = "gesture1";
	public const string defaultPostName = "posture1";
	
	/// default file name shown in the input textfield
	public static string defaultFileName = "wave";


	/// coefficients when training templates
	public const float angleThresMul = 3f;
	public const float coordThresMul = 3f;
	public const float timeThresMul = 10f;
	public const float minAngleThres = 20f;
	public const float minCoordThres = .3f;
	
	/// the joint indices which are used in template training by default
	///
	/// these joints will be used if you don't indicate other joints in training configuration,
	public static readonly Joint[] defaultDetectJointIdx = 
		{Joint.LeftShoulder, Joint.LeftElbow, 
		Joint.RightShoulder, Joint.RightElbow};
	
	/// the joint indices we want to record by SkeletonRecorder
	///
	/// Torso must be recorded to calibrate the attitude of the body.
	/// if elbows are recorded, then hands must also be recorded to calculate elbow's orientation. other joints are similar.
	public static readonly Joint[] defaultRecordJointIdx = 
		{Joint.LeftShoulder, Joint.LeftElbow, Joint.LeftHand,
		Joint.RightShoulder, Joint.RightElbow, Joint.RightHand, Joint.Torso};


	/// if the confidence of a joint is lower than minConfidence, then we won't do gesture detection
	public const float minConfidence = .1f;

	/// the minimum time interval in seconds between two recorded frames in SkeletonRecorder
	public const float minRecordIntv = 0.025f;
}

