using System.Runtime.InteropServices;

namespace SEClient.Tcp;

public struct Point2D
{
    public double X;
    public double Y;
};

public struct Vector2D
{
    public double X;
    public double Y;
};

public struct Point3D
{
    public double X;
    public double Y;
    public double Z;
};

public struct Vector3D
{
    public double X;
    public double Y;
    public double Z;
};

public struct String
{
    public ushort Size;
    public char[] Ptr;
    public readonly string AsString => new(Ptr);
    public readonly int StructSize => sizeof(ushort) + Size;
};

public struct WorldIntersection
{
    public Point3D WorldPoint;     // intersection point in world coordinates
    public Point3D ObjectPoint;    // intersection point in local object coordinates
    public String ObjectName;      // name of intersected object
    public readonly int StructSize => 2 * Marshal.SizeOf(typeof(Point3D)) + ObjectName.StructSize;
};

public struct Quaternion
{
    public double W;
    public double X;
    public double Y;
    public double Z;
};

public struct UserMarker
{
    /// <summary>
    /// Equal to 0 if no error, otherwise error.
    /// </summary>
    public int Error;
    /// <summary>
    /// CameraClock of this marker.
    /// </summary>
    public ulong CameraClock;
    /// <summary>
    /// Index of the camera that received this marker.
    /// </summary>
    public byte CameraIdx;
    /// <summary>
    /// User-defined data.
    /// </summary>
    public ulong Data;
};

public enum DataId : ushort
{
    //Frame Information
    FrameNumber = 0x0001,
    EstimatedDelay = 0x0002,
    TimeStamp = 0x0003,
    UserTimeStamp = 0x0004,
    FrameRate = 0x0005,
    CameraPositions = 0x0006,
    CameraRotations = 0x0007,
    UserDefinedData = 0x0008,
    RealTimeClock = 0x0009,
    KeyboardState = 0x0056,
    ASCIIKeyboardState = 0x00a4,
    UserMarker = 0x03a0,
    CameraClocks = 0x03a1,

    //Head Position
    HeadPosition = 0x0010,
    HeadPositionQ = 0x0011,
    HeadRotationRodrigues = 0x0012,
    HeadRotationQuaternion = 0x001d,
    HeadLeftEarDirection = 0x0015,
    HeadUpDirection = 0x0014,
    HeadNoseDirection = 0x0013,
    HeadHeading = 0x0016,
    HeadPitch = 0x0017,
    HeadRoll = 0x0018,
    HeadRotationQ = 0x0019,

    //Raw Gaze
    GazeOrigin = 0x001a,
    LeftGazeOrigin = 0x001b,
    RightGazeOrigin = 0x001c,
    EyePosition = 0x0020,
    GazeDirection = 0x0021,
    GazeDirectionQ = 0x0022,
    LeftEyePosition = 0x0023,
    LeftGazeDirection = 0x0024,
    LeftGazeDirectionQ = 0x0025,
    RightEyePosition = 0x0026,
    RightGazeDirection = 0x0027,
    RightGazeDirectionQ = 0x0028,
    GazeHeading = 0x0029,
    GazePitch = 0x002a,
    LeftGazeHeading = 0x002b,
    LeftGazePitch = 0x002c,
    RightGazeHeading = 0x002d,
    RightGazePitch = 0x002e,

    //Filtered Gaze
    FilteredGazeDirection = 0x0030,
    FilteredGazeDirectionQ = 0x0031,
    FilteredLeftGazeDirection = 0x0032,
    FilteredLeftGazeDirectionQ = 0x0033,
    FilteredRightGazeDirection = 0x0034,
    FilteredRightGazeDirectionQ = 0x0035,
    FilteredGazeHeading = 0x0036,
    FilteredGazePitch = 0x0037,
    FilteredLeftGazeHeading = 0x0038,
    FilteredLeftGazePitch = 0x0039,
    FilteredRightGazeHeading = 0x003a,
    FilteredRightGazePitch = 0x003b,

    //Analysis (non-real-time)
    Saccade = 0x003d,
    Fixation = 0x003e,
    Blink = 0x003f,
    LeftBlinkClosingMidTime = 0x00e0,
    LeftBlinkOpeningMidTime = 0x00e1,
    LeftBlinkClosingAmplitude = 0x00e2,
    LeftBlinkOpeningAmplitude = 0x00e3,
    LeftBlinkClosingSpeed = 0x00e4,
    LeftBlinkOpeningSpeed = 0x00e5,
    RightBlinkClosingMidTime = 0x00e6,
    RightBlinkOpeningMidTime = 0x00e7,
    RightBlinkClosingAmplitude = 0x00e8,
    RightBlinkOpeningAmplitude = 0x00e9,
    RightBlinkClosingSpeed = 0x00ea,
    RightBlinkOpeningSpeed = 0x00eb,

    //Intersections
    ClosestWorldIntersection = 0x0040,
    FilteredClosestWorldIntersection = 0x0041,
    AllWorldIntersections = 0x0042,
    FilteredAllWorldIntersections = 0x0043,
    ZoneId = 0x0044,
    EstimatedClosestWorldIntersection = 0x0045,
    EstimatedAllWorldIntersections = 0x0046,
    HeadClosestWorldIntersection = 0x0049,
    HeadAllWorldIntersections = 0x004a,
    CalibrationGazeIntersection = 0x00b0,
    TaggedGazeIntersection = 0x00b1,
    LeftClosestWorldIntersection = 0x00b2,
    LeftAllWorldIntersections = 0x00b3,
    RightClosestWorldIntersection = 0x00b4,
    RightAllWorldIntersections = 0x00b5,
    FilteredLeftClosestWorldIntersection = 0x00b6,
    FilteredLeftAllWorldIntersections = 0x00b7,
    FilteredRightClosestWorldIntersection = 0x00b8,
    FilteredRightAllWorldIntersections = 0x00b9,
    EstimatedLeftClosestWorldIntersection = 0x00ba,
    EstimatedLeftAllWorldIntersections = 0x00bb,
    EstimatedRightClosestWorldIntersection = 0x00bc,
    EstimatedRightAllWorldIntersections = 0x00bd,
    FilteredEstimatedClosestWorldIntersection = 0x0141,
    FilteredEstimatedAllWorldIntersections = 0x0143,
    FilteredEstimatedLeftClosestWorldIntersection = 0x01b6,
    FilteredEstimatedLeftAllWorldIntersections = 0x01b7,
    FilteredEstimatedRightClosestWorldIntersection = 0x01b8,
    FilteredEstimatedRightAllWorldIntersections = 0x01b9,

    //Eyelid
    EyelidOpening = 0x0050,
    EyelidOpeningQ = 0x0051,
    LeftEyelidOpening = 0x0052,
    LeftEyelidOpeningQ = 0x0053,
    RightEyelidOpening = 0x0054,
    RightEyelidOpeningQ = 0x0055,
    LeftLowerEyelidExtremePoint = 0x0058,
    LeftUpperEyelidExtremePoint = 0x0059,
    RightLowerEyelidExtremePoint = 0x005a,
    RightUpperEyelidExtremePoint = 0x005b,
    LeftEyelidState = 0x0390,
    RightEyelidState = 0x0391,

    //Pupilometry
    PupilDiameter = 0x0060,
    PupilDiameterQ = 0x0061,
    LeftPupilDiameter = 0x0062,
    LeftPupilDiameterQ = 0x0063,
    RightPupilDiameter = 0x0064,
    RightPupilDiameterQ = 0x0065,
    FilteredPupilDiameter = 0x0066,
    FilteredPupilDiameterQ = 0x0067,
    FilteredLeftPupilDiameter = 0x0068,
    FilteredLeftPupilDiameterQ = 0x0069,
    FilteredRightPupilDiameter = 0x006a,
    FilteredRightPupilDiameterQ = 0x006b,

    //GPS Information
    GPSPosition = 0x0070,
    GPSGroundSpeed = 0x0071,
    GPSCourse = 0x0072,
    GPSTime = 0x0073,

    //Raw Estimated Gaze
    EstimatedGazeOrigin = 0x007a,
    EstimatedLeftGazeOrigin = 0x007b,
    EstimatedRightGazeOrigin = 0x007c,
    EstimatedEyePosition = 0x0080,
    EstimatedGazeDirection = 0x0081,
    EstimatedGazeDirectionQ = 0x0082,
    EstimatedGazeHeading = 0x0083,
    EstimatedGazePitch = 0x0084,
    EstimatedLeftEyePosition = 0x0085,
    EstimatedLeftGazeDirection = 0x0086,
    EstimatedLeftGazeDirectionQ = 0x0087,
    EstimatedLeftGazeHeading = 0x0088,
    EstimatedLeftGazePitch = 0x0089,
    EstimatedRightEyePosition = 0x008a,
    EstimatedRightGazeDirection = 0x008b,
    EstimatedRightGazeDirectionQ = 0x008c,
    EstimatedRightGazeHeading = 0x008d,
    EstimatedRightGazePitch = 0x008e,

    //Filtered Estimated Gaze
    FilteredEstimatedGazeDirection = 0x0091,
    FilteredEstimatedGazeDirectionQ = 0x0092,
    FilteredEstimatedGazeHeading = 0x0093,
    FilteredEstimatedGazePitch = 0x0094,
    FilteredEstimatedLeftGazeDirection = 0x0096,
    FilteredEstimatedLeftGazeDirectionQ = 0x0097,
    FilteredEstimatedLeftGazeHeading = 0x0098,
    FilteredEstimatedLeftGazePitch = 0x0099,
    FilteredEstimatedRightGazeDirection = 0x009b,
    FilteredEstimatedRightGazeDirectionQ = 0x009c,
    FilteredEstimatedRightGazeHeading = 0x009d,
    FilteredEstimatedRightGazePitch = 0x009e,

    //Status
    TrackingState = 0x00c0,
    EyeglassesStatus = 0x00c1,
    ReflexReductionStateDEPRECATED = 0x00c2,

    //Facial Feature Positions
    LeftEyeOuterCorner3D = 0x0300,
    LeftEyeInnerCorner3D = 0x0301,
    RightEyeInnerCorner3D = 0x0302,
    RightEyeOuterCorner3D = 0x0303,
    LeftNostril3D = 0x0304,
    RightNostril3D = 0x0305,
    LeftMouthCorner3D = 0x0306,
    RightMouthCorner3D = 0x0307,
    LeftEar3D = 0x0308,
    RightEar3D = 0x0309,
    NoseTip3D = 0x0360,
    LeftEyeOuterCorner2D = 0x0310,
    LeftEyeInnerCorner2D = 0x0311,
    RightEyeInnerCorner2D = 0x0312,
    RightEyeOuterCorner2D = 0x0313,
    LeftNostril2D = 0x0314,
    RightNostril2D = 0x0315,
    LeftMouthCorner2D = 0x0316,
    RightMouthCorner2D = 0x0317,
    LeftEar2D = 0x0318,
    RightEar2D = 0x0319,
    NoseTip2D = 0x0370,

    //Emotion
    EmotionJoy = 0x03b0,
    EmotionFear = 0x03b1,
    EmotionDisgust = 0x03b2,
    EmotionSadness = 0x03b3,
    EmotionSurprise = 0x03b5,
    EmotionValence = 0x03b7,
    EmotionEngagement = 0x03b8,
    EmotionSentimentality = 0x03b9,
    EmotionConfusion = 0x03ba,
    EmotionNeutral = 0x03bb,
    EmotionQ = 0x03bc,

    //Expression
    ExpressionSmile = 0x03c0,
    ExpressionInnerBrowRaise = 0x03c1,
    ExpressionBrowRaise = 0x03c2,
    ExpressionBrowFurrow = 0x03c3,
    ExpressionNoseWrinkle = 0x03c4,
    ExpressionUpperLipRaise = 0x03c5,
    ExpressionLipCornerDepressor = 0x03c6,
    ExpressionChinRaise = 0x03c7,
    ExpressionLipPucker = 0x03c8,
    ExpressionLipPress = 0x03c9,
    ExpressionLipSuck = 0x03ca,
    ExpressionMouthOpen = 0x03cb,
    ExpressionSmirk = 0x03d0,
    ExpressionAttention = 0x03d3,
    ExpressionEyeWiden = 0x03d4,
    ExpressionCheekRaise = 0x03d5,
    ExpressionLidTighten = 0x03d6,
    ExpressionDimpler = 0x03d7,
    ExpressionLipStretch = 0x03d8,
    ExpressionJawDrop = 0x03d9,
    ExpressionQ = 0x03e0,

    //0x0200 - 0x0202 cannot be used
};

public record struct Data
{
    public ushort Size;
    public ushort PacketCount;

    //Frame Information
    public uint? FrameNumber;
    public uint? EstimatedDelay;
    public ulong? TimeStamp;
    public ulong? UserTimeStamp;
    public double? FrameRate;
    public ushort[]? CameraPositions;
    public ushort[]? CameraRotations;
    public ulong? UserDefinedData;
    public ulong? RealTimeClock;
    public String? KeyboardState;
    public ushort? ASCIIKeyboardState;
    public UserMarker? UserMarker;
    public ushort[]? CameraClocks;

    //Head Position
    public Point3D? HeadPosition;
    public double? HeadPositionQ;
    public Vector3D? HeadRotationRodrigues;
    public Quaternion? HeadRotationQuaternion;
    public Vector3D? HeadLeftEarDirection;
    public Vector3D? HeadUpDirection;
    public Vector3D? HeadNoseDirection;
    public double? HeadHeading;
    public double? HeadPitch;
    public double? HeadRoll;
    public double? HeadRotationQ;

    //Raw Gaze
    public Point3D? GazeOrigin;
    public Point3D? LeftGazeOrigin;
    public Point3D? RightGazeOrigin;
    public Point3D? EyePosition;
    public Vector3D? GazeDirection;
    public double? GazeDirectionQ;
    public Point3D? LeftEyePosition;
    public Vector3D? LeftGazeDirection;
    public double? LeftGazeDirectionQ;
    public Point3D? RightEyePosition;
    public Vector3D? RightGazeDirection;
    public double? RightGazeDirectionQ;
    public double? GazeHeading;
    public double? GazePitch;
    public double? LeftGazeHeading;
    public double? LeftGazePitch;
    public double? RightGazeHeading;
    public double? RightGazePitch;

    //Filtered Gaze
    public Vector3D? FilteredGazeDirection;
    public double? FilteredGazeDirectionQ;
    public Vector3D? FilteredLeftGazeDirection;
    public double? FilteredLeftGazeDirectionQ;
    public Vector3D? FilteredRightGazeDirection;
    public double? FilteredRightGazeDirectionQ;
    public double? FilteredGazeHeading;
    public double? FilteredGazePitch;
    public double? FilteredLeftGazeHeading;
    public double? FilteredLeftGazePitch;
    public double? FilteredRightGazeHeading;
    public double? FilteredRightGazePitch;

    //Analysis (non-real-time)
    public uint? Saccade;
    public uint? Fixation;
    public uint? Blink;
    public ulong? LeftBlinkClosingMidTime;
    public ulong? LeftBlinkOpeningMidTime;
    public double? LeftBlinkClosingAmplitude;
    public double? LeftBlinkOpeningAmplitude;
    public double? LeftBlinkClosingSpeed;
    public double? LeftBlinkOpeningSpeed;
    public ulong? RightBlinkClosingMidTime;
    public ulong? RightBlinkOpeningMidTime;
    public double? RightBlinkClosingAmplitude;
    public double? RightBlinkOpeningAmplitude;
    public double? RightBlinkClosingSpeed;
    public double? RightBlinkOpeningSpeed;

    //Intersections
    public WorldIntersection? ClosestWorldIntersection;
    public WorldIntersection? FilteredClosestWorldIntersection;
    public WorldIntersection[]? AllWorldIntersections;
    public WorldIntersection[]? FilteredAllWorldIntersections;
    public ushort? ZoneId;
    public WorldIntersection? EstimatedClosestWorldIntersection;
    public WorldIntersection[]? EstimatedAllWorldIntersections;
    public WorldIntersection? HeadClosestWorldIntersection;
    public WorldIntersection[]? HeadAllWorldIntersections;
    public WorldIntersection? CalibrationGazeIntersection;
    public WorldIntersection? TaggedGazeIntersection;
    public WorldIntersection? LeftClosestWorldIntersection;
    public WorldIntersection[]? LeftAllWorldIntersections;
    public WorldIntersection? RightClosestWorldIntersection;
    public WorldIntersection[]? RightAllWorldIntersections;
    public WorldIntersection? FilteredLeftClosestWorldIntersection;
    public WorldIntersection[]? FilteredLeftAllWorldIntersections;
    public WorldIntersection? FilteredRightClosestWorldIntersection;
    public WorldIntersection[]? FilteredRightAllWorldIntersections;
    public WorldIntersection? EstimatedLeftClosestWorldIntersection;
    public WorldIntersection[]? EstimatedLeftAllWorldIntersections;
    public WorldIntersection? EstimatedRightClosestWorldIntersection;
    public WorldIntersection[]? EstimatedRightAllWorldIntersections;
    public WorldIntersection? FilteredEstimatedClosestWorldIntersection;
    public WorldIntersection[]? FilteredEstimatedAllWorldIntersections;
    public WorldIntersection? FilteredEstimatedLeftClosestWorldIntersection;
    public WorldIntersection[]? FilteredEstimatedLeftAllWorldIntersections;
    public WorldIntersection? FilteredEstimatedRightClosestWorldIntersection;
    public WorldIntersection[]? FilteredEstimatedRightAllWorldIntersections;

    //Eyelid
    public double? EyelidOpening;
    public double? EyelidOpeningQ;
    public double? LeftEyelidOpening;
    public double? LeftEyelidOpeningQ;
    public double? RightEyelidOpening;
    public double? RightEyelidOpeningQ;
    public Point3D? LeftLowerEyelidExtremePoint;
    public Point3D? LeftUpperEyelidExtremePoint;
    public Point3D? RightLowerEyelidExtremePoint;
    public Point3D? RightUpperEyelidExtremePoint;
    public byte? LeftEyelidState;
    public byte? RightEyelidState;

    //Pupilometry
    public double? PupilDiameter;
    public double? PupilDiameterQ;
    public double? LeftPupilDiameter;
    public double? LeftPupilDiameterQ;
    public double? RightPupilDiameter;
    public double? RightPupilDiameterQ;
    public double? FilteredPupilDiameter;
    public double? FilteredPupilDiameterQ;
    public double? FilteredLeftPupilDiameter;
    public double? FilteredLeftPupilDiameterQ;
    public double? FilteredRightPupilDiameter;
    public double? FilteredRightPupilDiameterQ;

    //GPS Information
    public Point2D? GPSPosition;
    public double? GPSGroundSpeed;
    public double? GPSCourse;
    public ulong? GPSTime;

    //Raw Estimated Gaze
    public Point3D? EstimatedGazeOrigin;
    public Point3D? EstimatedLeftGazeOrigin;
    public Point3D? EstimatedRightGazeOrigin;
    public Point3D? EstimatedEyePosition;
    public Vector3D? EstimatedGazeDirection;
    public double? EstimatedGazeDirectionQ;
    public double? EstimatedGazeHeading;
    public double? EstimatedGazePitch;
    public Point3D? EstimatedLeftEyePosition;
    public Vector3D? EstimatedLeftGazeDirection;
    public double? EstimatedLeftGazeDirectionQ;
    public double? EstimatedLeftGazeHeading;
    public double? EstimatedLeftGazePitch;
    public Point3D? EstimatedRightEyePosition;
    public Vector3D? EstimatedRightGazeDirection;
    public double? EstimatedRightGazeDirectionQ;
    public double? EstimatedRightGazeHeading;
    public double? EstimatedRightGazePitch;

    //Filtered Estimated Gaze
    public Vector3D? FilteredEstimatedGazeDirection;
    public double? FilteredEstimatedGazeDirectionQ;
    public double? FilteredEstimatedGazeHeading;
    public double? FilteredEstimatedGazePitch;
    public Vector3D? FilteredEstimatedLeftGazeDirection;
    public double? FilteredEstimatedLeftGazeDirectionQ;
    public double? FilteredEstimatedLeftGazeHeading;
    public double? FilteredEstimatedLeftGazePitch;
    public Vector3D? FilteredEstimatedRightGazeDirection;
    public double? FilteredEstimatedRightGazeDirectionQ;
    public double? FilteredEstimatedRightGazeHeading;
    public double? FilteredEstimatedRightGazePitch;

    //Status
    public byte? TrackingState;
    public byte? EyeglassesStatus;
    public byte? ReflexReductionStateDEPRECATED;

    //Facial Feature Positions
    public Point3D? LeftEyeOuterCorner3D;
    public Point3D? LeftEyeInnerCorner3D;
    public Point3D? RightEyeInnerCorner3D;
    public Point3D? RightEyeOuterCorner3D;
    public Point3D? LeftNostril3D;
    public Point3D? RightNostril3D;
    public Point3D? LeftMouthCorner3D;
    public Point3D? RightMouthCorner3D;
    public Point3D? LeftEar3D;
    public Point3D? RightEar3D;
    public Point3D? NoseTip3D;
    public ushort[]? LeftEyeOuterCorner2D;
    public ushort[]? LeftEyeInnerCorner2D;
    public ushort[]? RightEyeInnerCorner2D;
    public ushort[]? RightEyeOuterCorner2D;
    public ushort[]? LeftNostril2D;
    public ushort[]? RightNostril2D;
    public ushort[]? LeftMouthCorner2D;
    public ushort[]? RightMouthCorner2D;
    public ushort[]? LeftEar2D;
    public ushort[]? RightEar2D;
    public ushort[]? NoseTip2D;

    //Emotion
    public double? EmotionJoy;
    public double? EmotionFear;
    public double? EmotionDisgust;
    public double? EmotionSadness;
    public double? EmotionSurprise;
    public double? EmotionValence;
    public double? EmotionEngagement;
    public double? EmotionSentimentality;
    public double? EmotionConfusion;
    public double? EmotionNeutral;
    public double? EmotionQ;

    //Expression
    public double? ExpressionSmile;
    public double? ExpressionInnerBrowRaise;
    public double? ExpressionBrowRaise;
    public double? ExpressionBrowFurrow;
    public double? ExpressionNoseWrinkle;
    public double? ExpressionUpperLipRaise;
    public double? ExpressionLipCornerDepressor;
    public double? ExpressionChinRaise;
    public double? ExpressionLipPucker;
    public double? ExpressionLipPress;
    public double? ExpressionLipSuck;
    public double? ExpressionMouthOpen;
    public double? ExpressionSmirk;
    public double? ExpressionAttention;
    public double? ExpressionEyeWiden;
    public double? ExpressionCheekRaise;
    public double? ExpressionLidTighten;
    public double? ExpressionDimpler;
    public double? ExpressionLipStretch;
    public double? ExpressionJawDrop;
    public double? ExpressionQ;
};

// Assembly-only

internal struct PacketHeader
{
    /// <summary>
    /// Always 'SEPD'
    /// </summary>
    public uint SyncId;
    /// <summary>
    /// Always 4
    /// </summary>
    public ushort PacketType;
    /// <summary>
    /// Number of bytes following this header, that is, not including size of this header
    /// </summary>
    public ushort Length;
};

internal struct SubPacketHeader
{
    /// <summary>
    /// Output data identifier, refer to <see cref="DataId"/> for existing ids
    /// </summary>
    public DataId Id;
    /// <summary>
    /// Number of bytes following this header
    /// </summary>
    public ushort Length;
};
