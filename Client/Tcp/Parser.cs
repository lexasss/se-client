using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SEClient.Tcp;

internal static class Parser
{
    public static PacketHeader ReadHeader(NetworkStream stream)
    {
        PacketHeader header = new() { SyncId = 0x44504553, PacketType = 4, Length = 0 };

        byte[] buffer = new byte[16];
        int offset = 0;

        while (stream.Read(buffer, 0, 1) != 0)
        {
            if (buffer[0] == HEADER_START[offset])
            {
                offset += 1;
                if (offset == HEADER_START.Length)
                {
                    if (stream.Read(buffer, 0, sizeof(ushort)) == sizeof(ushort))
                    {
                        header.Length = new ItoH16(buffer).UInt;
                    }

                    break;
                }
            }
            else if (offset > 0)
            {
                Debug.WriteLine($"[{nameof(Parser)}] wrong signature: got '{buffer[0]}', expected '{HEADER_START[offset]}'");
                offset = 0;
            }
        }

        return header;
    }

    public static Data? ReadData(NetworkStream stream, ushort length)
    {
        var data = new Data();

        int bytesRead = 0;
        ushort packetCount = 0;
        while (bytesRead < length)
        {
            int count = ReadSubHeader(stream, out SubPacketHeader subHeader);
            bytesRead += count;
            if (count == 0)
                return null;

            byte[] subPayload = new byte[subHeader.Length];
            count = stream.Read(subPayload, 0, subPayload.Length);
            bytesRead += count;
            if (count == 0)
                return null;

            packetCount += 1;
            DecodeData(ref data, subHeader.Id, subPayload);
        }

        if (bytesRead >= length)
        {
            data.PacketCount = packetCount;
        }
        else
        {
            data.PacketCount = 0;
        }

        return data;
    }

    // Internal 

    static readonly char[] HEADER_START = new char[] { 'S', 'E', 'P', 'D', '\x00', '\x04' };

    private static int ReadSubHeader(NetworkStream stream, out SubPacketHeader subHeader)
    {
        subHeader = new();

        byte[] buffer = new byte[16];
        int bytesRead = 0;
        int count;

        if ((count = stream.Read(buffer, 0, sizeof(ushort))) == 0)
            return 0;

        bytesRead += count;
        subHeader.Id = (DataId)new ItoH16(buffer).UInt;

        if ((count = stream.Read(buffer, 0, sizeof(ushort))) == 0)
            return 0;

        bytesRead += count;
        subHeader.Length = new ItoH16(buffer).UInt;

        return bytesRead;
    }

    private static void DecodeData(ref Data sample, DataId id, byte[] data)
    {
        switch (id)
        {
            case DataId.FrameNumber: sample.FrameNumber = new ItoH32(data).UInt; break;
            case DataId.EstimatedDelay: sample.EstimatedDelay = new ItoH32(data).UInt; break;
            case DataId.TimeStamp: sample.TimeStamp = new ItoH64(data).UInt; break;
            case DataId.UserTimeStamp: sample.UserTimeStamp = new ItoH64(data).UInt; break;
            case DataId.FrameRate: sample.FrameRate = new ItoH64(data).Float; break;
            case DataId.CameraPositions: sample.CameraPositions = DecodeVector(data); break;
            case DataId.CameraRotations: sample.CameraRotations = DecodeVector(data); break;
            case DataId.UserDefinedData: sample.UserDefinedData = new ItoH64(data).UInt; break;
            case DataId.RealTimeClock: sample.RealTimeClock = new ItoH64(data).UInt; break;
            case DataId.KeyboardState: sample.KeyboardState = DecodeString(data); break;
            case DataId.ASCIIKeyboardState: sample.ASCIIKeyboardState = new ItoH16(data).UInt; break;
            case DataId.UserMarker: sample.UserMarker = DecodeMarker(data); break;
            case DataId.CameraClocks: sample.CameraClocks = DecodeVector(data); break;

            //Head Position
            case DataId.HeadPosition: sample.HeadPosition = DecodePoint3D(data); break;
            case DataId.HeadPositionQ: sample.HeadPositionQ = new ItoH64(data).Float; break;
            case DataId.HeadRotationRodrigues: sample.HeadRotationRodrigues = DecodeVector3D(data); break;
            case DataId.HeadRotationQuaternion: sample.HeadRotationQuaternion = DecodeQuaternion(data); break;
            case DataId.HeadLeftEarDirection: sample.HeadLeftEarDirection = DecodeVector3D(data); break;
            case DataId.HeadUpDirection: sample.HeadUpDirection = DecodeVector3D(data); break;
            case DataId.HeadNoseDirection: sample.HeadNoseDirection = DecodeVector3D(data); break;
            case DataId.HeadHeading: sample.HeadHeading = new ItoH64(data).Float; break;
            case DataId.HeadPitch: sample.HeadPitch = new ItoH64(data).Float; break;
            case DataId.HeadRoll: sample.HeadRoll = new ItoH64(data).Float; break;
            case DataId.HeadRotationQ: sample.HeadRotationQ = new ItoH64(data).Float; break;

            //Raw Gaze
            case DataId.GazeOrigin: sample.GazeOrigin = DecodePoint3D(data); break;
            case DataId.LeftGazeOrigin: sample.LeftGazeOrigin = DecodePoint3D(data); break;
            case DataId.RightGazeOrigin: sample.RightGazeOrigin = DecodePoint3D(data); break;
            case DataId.EyePosition: sample.EyePosition = DecodePoint3D(data); break;
            case DataId.GazeDirection: sample.GazeDirection = DecodeVector3D(data); break;
            case DataId.GazeDirectionQ: sample.GazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.LeftEyePosition: sample.LeftEyePosition = DecodePoint3D(data); break;
            case DataId.LeftGazeDirection: sample.LeftGazeDirection = DecodeVector3D(data); break;
            case DataId.LeftGazeDirectionQ: sample.LeftGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.RightEyePosition: sample.RightEyePosition = DecodePoint3D(data); break;
            case DataId.RightGazeDirection: sample.RightGazeDirection = DecodeVector3D(data); break;
            case DataId.RightGazeDirectionQ: sample.RightGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.GazeHeading: sample.GazeHeading = new ItoH64(data).Float; break;
            case DataId.GazePitch: sample.GazePitch = new ItoH64(data).Float; break;
            case DataId.LeftGazeHeading: sample.LeftGazeHeading = new ItoH64(data).Float; break;
            case DataId.LeftGazePitch: sample.LeftGazePitch = new ItoH64(data).Float; break;
            case DataId.RightGazeHeading: sample.RightGazeHeading = new ItoH64(data).Float; break;
            case DataId.RightGazePitch: sample.RightGazePitch = new ItoH64(data).Float; break;

            //Filtered Gaze
            case DataId.FilteredGazeDirection: sample.FilteredGazeDirection = DecodeVector3D(data); break;
            case DataId.FilteredGazeDirectionQ: sample.FilteredGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.FilteredLeftGazeDirection: sample.FilteredLeftGazeDirection = DecodeVector3D(data); break;
            case DataId.FilteredLeftGazeDirectionQ: sample.FilteredLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.FilteredRightGazeDirection: sample.FilteredRightGazeDirection = DecodeVector3D(data); break;
            case DataId.FilteredRightGazeDirectionQ: sample.FilteredRightGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.FilteredGazeHeading: sample.FilteredGazeHeading = new ItoH64(data).Float; break;
            case DataId.FilteredGazePitch: sample.FilteredGazePitch = new ItoH64(data).Float; break;
            case DataId.FilteredLeftGazeHeading: sample.FilteredLeftGazeHeading = new ItoH64(data).Float; break;
            case DataId.FilteredLeftGazePitch: sample.FilteredLeftGazePitch = new ItoH64(data).Float; break;
            case DataId.FilteredRightGazeHeading: sample.FilteredRightGazeHeading = new ItoH64(data).Float; break;
            case DataId.FilteredRightGazePitch: sample.FilteredRightGazePitch = new ItoH64(data).Float; break;

            //Analysis (non-real-time)
            case DataId.Saccade: sample.Saccade = new ItoH32(data).UInt; break;
            case DataId.Fixation: sample.Fixation = new ItoH32(data).UInt; break;
            case DataId.Blink: sample.Blink = new ItoH32(data).UInt; break;
            case DataId.LeftBlinkClosingMidTime: sample.LeftBlinkClosingMidTime = new ItoH64(data).UInt; break;
            case DataId.LeftBlinkOpeningMidTime: sample.LeftBlinkOpeningMidTime = new ItoH64(data).UInt; break;
            case DataId.LeftBlinkClosingAmplitude: sample.LeftBlinkClosingAmplitude = new ItoH64(data).Float; break;
            case DataId.LeftBlinkOpeningAmplitude: sample.LeftBlinkOpeningAmplitude = new ItoH64(data).Float; break;
            case DataId.LeftBlinkClosingSpeed: sample.LeftBlinkClosingSpeed = new ItoH64(data).Float; break;
            case DataId.LeftBlinkOpeningSpeed: sample.LeftBlinkOpeningSpeed = new ItoH64(data).Float; break;
            case DataId.RightBlinkClosingMidTime: sample.RightBlinkClosingMidTime = new ItoH64(data).UInt; break;
            case DataId.RightBlinkOpeningMidTime: sample.RightBlinkOpeningMidTime = new ItoH64(data).UInt; break;
            case DataId.RightBlinkClosingAmplitude: sample.RightBlinkClosingAmplitude = new ItoH64(data).Float; break;
            case DataId.RightBlinkOpeningAmplitude: sample.RightBlinkOpeningAmplitude = new ItoH64(data).Float; break;
            case DataId.RightBlinkClosingSpeed: sample.RightBlinkClosingSpeed = new ItoH64(data).Float; break;
            case DataId.RightBlinkOpeningSpeed: sample.RightBlinkOpeningSpeed = new ItoH64(data).Float; break;

            //Intersections
            case DataId.ClosestWorldIntersection: sample.ClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.FilteredClosestWorldIntersection: sample.FilteredClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.AllWorldIntersections: sample.AllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.FilteredAllWorldIntersections: sample.FilteredAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.ZoneId: sample.ZoneId = new ItoH16(data).UInt; break;
            case DataId.EstimatedClosestWorldIntersection: sample.EstimatedClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.EstimatedAllWorldIntersections: sample.EstimatedAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.HeadClosestWorldIntersection: sample.HeadClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.HeadAllWorldIntersections: sample.HeadAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.CalibrationGazeIntersection: sample.CalibrationGazeIntersection = DecodeWorldIntersection(data); break;
            case DataId.TaggedGazeIntersection: sample.TaggedGazeIntersection = DecodeWorldIntersection(data); break;
            case DataId.LeftClosestWorldIntersection: sample.LeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.LeftAllWorldIntersections: sample.LeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.RightClosestWorldIntersection: sample.RightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.RightAllWorldIntersections: sample.RightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.FilteredLeftClosestWorldIntersection: sample.FilteredLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.FilteredLeftAllWorldIntersections: sample.FilteredLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.FilteredRightClosestWorldIntersection: sample.FilteredRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.FilteredRightAllWorldIntersections: sample.FilteredRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.EstimatedLeftClosestWorldIntersection: sample.EstimatedLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.EstimatedLeftAllWorldIntersections: sample.EstimatedLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.EstimatedRightClosestWorldIntersection: sample.EstimatedRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.EstimatedRightAllWorldIntersections: sample.EstimatedRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.FilteredEstimatedClosestWorldIntersection: sample.FilteredEstimatedClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.FilteredEstimatedAllWorldIntersections: sample.FilteredEstimatedAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.FilteredEstimatedLeftClosestWorldIntersection: sample.FilteredEstimatedLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.FilteredEstimatedLeftAllWorldIntersections: sample.FilteredEstimatedLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case DataId.FilteredEstimatedRightClosestWorldIntersection: sample.FilteredEstimatedRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case DataId.FilteredEstimatedRightAllWorldIntersections: sample.FilteredEstimatedRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;

            //Eyelid
            case DataId.EyelidOpening: sample.EyelidOpening = new ItoH64(data).Float; break;
            case DataId.EyelidOpeningQ: sample.EyelidOpeningQ = new ItoH64(data).Float; break;
            case DataId.LeftEyelidOpening: sample.LeftEyelidOpening = new ItoH64(data).Float; break;
            case DataId.LeftEyelidOpeningQ: sample.LeftEyelidOpeningQ = new ItoH64(data).Float; break;
            case DataId.RightEyelidOpening: sample.RightEyelidOpening = new ItoH64(data).Float; break;
            case DataId.RightEyelidOpeningQ: sample.RightEyelidOpeningQ = new ItoH64(data).Float; break;
            case DataId.LeftLowerEyelidExtremePoint: sample.LeftLowerEyelidExtremePoint = DecodePoint3D(data); break;
            case DataId.LeftUpperEyelidExtremePoint: sample.LeftUpperEyelidExtremePoint = DecodePoint3D(data); break;
            case DataId.RightLowerEyelidExtremePoint: sample.RightLowerEyelidExtremePoint = DecodePoint3D(data); break;
            case DataId.RightUpperEyelidExtremePoint: sample.RightUpperEyelidExtremePoint = DecodePoint3D(data); break;
            case DataId.LeftEyelidState: sample.LeftEyelidState = data[0]; break;
            case DataId.RightEyelidState: sample.RightEyelidState = data[0]; break;

            //Pupilometry
            case DataId.PupilDiameter: sample.PupilDiameter = new ItoH64(data).Float; break;
            case DataId.PupilDiameterQ: sample.PupilDiameterQ = new ItoH64(data).Float; break;
            case DataId.LeftPupilDiameter: sample.LeftPupilDiameter = new ItoH64(data).Float; break;
            case DataId.LeftPupilDiameterQ: sample.LeftPupilDiameterQ = new ItoH64(data).Float; break;
            case DataId.RightPupilDiameter: sample.RightPupilDiameter = new ItoH64(data).Float; break;
            case DataId.RightPupilDiameterQ: sample.RightPupilDiameterQ = new ItoH64(data).Float; break;
            case DataId.FilteredPupilDiameter: sample.FilteredPupilDiameter = new ItoH64(data).Float; break;
            case DataId.FilteredPupilDiameterQ: sample.FilteredPupilDiameterQ = new ItoH64(data).Float; break;
            case DataId.FilteredLeftPupilDiameter: sample.FilteredLeftPupilDiameter = new ItoH64(data).Float; break;
            case DataId.FilteredLeftPupilDiameterQ: sample.FilteredLeftPupilDiameterQ = new ItoH64(data).Float; break;
            case DataId.FilteredRightPupilDiameter: sample.FilteredRightPupilDiameter = new ItoH64(data).Float; break;
            case DataId.FilteredRightPupilDiameterQ: sample.FilteredRightPupilDiameterQ = new ItoH64(data).Float; break;

            //GPS Information
            case DataId.GPSPosition: sample.GPSPosition = DecodePoint2D(data); break;
            case DataId.GPSGroundSpeed: sample.GPSGroundSpeed = new ItoH64(data).Float; break;
            case DataId.GPSCourse: sample.GPSCourse = new ItoH64(data).Float; break;
            case DataId.GPSTime: sample.GPSTime = new ItoH64(data).UInt; break;

            //Raw Estimated Gaze
            case DataId.EstimatedGazeOrigin: sample.EstimatedGazeOrigin = DecodePoint3D(data); break;
            case DataId.EstimatedLeftGazeOrigin: sample.EstimatedLeftGazeOrigin = DecodePoint3D(data); break;
            case DataId.EstimatedRightGazeOrigin: sample.EstimatedRightGazeOrigin = DecodePoint3D(data); break;
            case DataId.EstimatedEyePosition: sample.EstimatedEyePosition = DecodePoint3D(data); break;
            case DataId.EstimatedGazeDirection: sample.EstimatedGazeDirection = DecodeVector3D(data); break;
            case DataId.EstimatedGazeDirectionQ: sample.EstimatedGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.EstimatedGazeHeading: sample.EstimatedGazeHeading = new ItoH64(data).Float; break;
            case DataId.EstimatedGazePitch: sample.EstimatedGazePitch = new ItoH64(data).Float; break;
            case DataId.EstimatedLeftEyePosition: sample.EstimatedLeftEyePosition = DecodePoint3D(data); break;
            case DataId.EstimatedLeftGazeDirection: sample.EstimatedLeftGazeDirection = DecodeVector3D(data); break;
            case DataId.EstimatedLeftGazeDirectionQ: sample.EstimatedLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.EstimatedLeftGazeHeading: sample.EstimatedLeftGazeHeading = new ItoH64(data).Float; break;
            case DataId.EstimatedLeftGazePitch: sample.EstimatedLeftGazePitch = new ItoH64(data).Float; break;
            case DataId.EstimatedRightEyePosition: sample.EstimatedRightEyePosition = DecodePoint3D(data); break;
            case DataId.EstimatedRightGazeDirection: sample.EstimatedRightGazeDirection = DecodeVector3D(data); break;
            case DataId.EstimatedRightGazeDirectionQ: sample.EstimatedRightGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.EstimatedRightGazeHeading: sample.EstimatedRightGazeHeading = new ItoH64(data).Float; break;
            case DataId.EstimatedRightGazePitch: sample.EstimatedRightGazePitch = new ItoH64(data).Float; break;

            //Filtered Estimated Gaze
            case DataId.FilteredEstimatedGazeDirection: sample.FilteredEstimatedGazeDirection = DecodeVector3D(data); break;
            case DataId.FilteredEstimatedGazeDirectionQ: sample.FilteredEstimatedGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedGazeHeading: sample.FilteredEstimatedGazeHeading = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedGazePitch: sample.FilteredEstimatedGazePitch = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedLeftGazeDirection: sample.FilteredEstimatedLeftGazeDirection = DecodeVector3D(data); break;
            case DataId.FilteredEstimatedLeftGazeDirectionQ: sample.FilteredEstimatedLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedLeftGazeHeading: sample.FilteredEstimatedLeftGazeHeading = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedLeftGazePitch: sample.FilteredEstimatedLeftGazePitch = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedRightGazeDirection: sample.FilteredEstimatedRightGazeDirection = DecodeVector3D(data); break;
            case DataId.FilteredEstimatedRightGazeDirectionQ: sample.FilteredEstimatedRightGazeDirectionQ = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedRightGazeHeading: sample.FilteredEstimatedRightGazeHeading = new ItoH64(data).Float; break;
            case DataId.FilteredEstimatedRightGazePitch: sample.FilteredEstimatedRightGazePitch = new ItoH64(data).Float; break;

            //Status
            case DataId.TrackingState: sample.TrackingState = data[0]; break;
            case DataId.EyeglassesStatus: sample.EyeglassesStatus = data[0]; break;
            case DataId.ReflexReductionStateDEPRECATED: sample.ReflexReductionStateDEPRECATED = data[0]; break;

            //Facial Feature Positions
            case DataId.LeftEyeOuterCorner3D: sample.LeftEyeOuterCorner3D = DecodePoint3D(data); break;
            case DataId.LeftEyeInnerCorner3D: sample.LeftEyeInnerCorner3D = DecodePoint3D(data); break;
            case DataId.RightEyeInnerCorner3D: sample.RightEyeInnerCorner3D = DecodePoint3D(data); break;
            case DataId.RightEyeOuterCorner3D: sample.RightEyeOuterCorner3D = DecodePoint3D(data); break;
            case DataId.LeftNostril3D: sample.LeftNostril3D = DecodePoint3D(data); break;
            case DataId.RightNostril3D: sample.RightNostril3D = DecodePoint3D(data); break;
            case DataId.LeftMouthCorner3D: sample.LeftMouthCorner3D = DecodePoint3D(data); break;
            case DataId.RightMouthCorner3D: sample.RightMouthCorner3D = DecodePoint3D(data); break;
            case DataId.LeftEar3D: sample.LeftEar3D = DecodePoint3D(data); break;
            case DataId.RightEar3D: sample.RightEar3D = DecodePoint3D(data); break;
            case DataId.NoseTip3D: sample.NoseTip3D = DecodePoint3D(data); break;
            case DataId.LeftEyeOuterCorner2D: sample.LeftEyeOuterCorner2D = DecodeVector(data); break;
            case DataId.LeftEyeInnerCorner2D: sample.LeftEyeInnerCorner2D = DecodeVector(data); break;
            case DataId.RightEyeInnerCorner2D: sample.RightEyeInnerCorner2D = DecodeVector(data); break;
            case DataId.RightEyeOuterCorner2D: sample.RightEyeOuterCorner2D = DecodeVector(data); break;
            case DataId.LeftNostril2D: sample.LeftNostril2D = DecodeVector(data); break;
            case DataId.RightNostril2D: sample.RightNostril2D = DecodeVector(data); break;
            case DataId.LeftMouthCorner2D: sample.LeftMouthCorner2D = DecodeVector(data); break;
            case DataId.RightMouthCorner2D: sample.RightMouthCorner2D = DecodeVector(data); break;
            case DataId.LeftEar2D: sample.LeftEar2D = DecodeVector(data); break;
            case DataId.RightEar2D: sample.RightEar2D = DecodeVector(data); break;
            case DataId.NoseTip2D: sample.NoseTip2D = DecodeVector(data); break;

            //Emotion
            case DataId.EmotionJoy: sample.EmotionJoy = new ItoH64(data).Float; break;
            case DataId.EmotionFear: sample.EmotionFear = new ItoH64(data).Float; break;
            case DataId.EmotionDisgust: sample.EmotionDisgust = new ItoH64(data).Float; break;
            case DataId.EmotionSadness: sample.EmotionSadness = new ItoH64(data).Float; break;
            case DataId.EmotionSurprise: sample.EmotionSurprise = new ItoH64(data).Float; break;
            case DataId.EmotionValence: sample.EmotionValence = new ItoH64(data).Float; break;
            case DataId.EmotionEngagement: sample.EmotionEngagement = new ItoH64(data).Float; break;
            case DataId.EmotionSentimentality: sample.EmotionSentimentality = new ItoH64(data).Float; break;
            case DataId.EmotionConfusion: sample.EmotionConfusion = new ItoH64(data).Float; break;
            case DataId.EmotionNeutral: sample.EmotionNeutral = new ItoH64(data).Float; break;
            case DataId.EmotionQ: sample.EmotionQ = new ItoH64(data).Float; break;

            //Expression
            case DataId.ExpressionSmile: sample.ExpressionSmile = new ItoH64(data).Float; break;
            case DataId.ExpressionInnerBrowRaise: sample.ExpressionInnerBrowRaise = new ItoH64(data).Float; break;
            case DataId.ExpressionBrowRaise: sample.ExpressionBrowRaise = new ItoH64(data).Float; break;
            case DataId.ExpressionBrowFurrow: sample.ExpressionBrowFurrow = new ItoH64(data).Float; break;
            case DataId.ExpressionNoseWrinkle: sample.ExpressionNoseWrinkle = new ItoH64(data).Float; break;
            case DataId.ExpressionUpperLipRaise: sample.ExpressionUpperLipRaise = new ItoH64(data).Float; break;
            case DataId.ExpressionLipCornerDepressor: sample.ExpressionLipCornerDepressor = new ItoH64(data).Float; break;
            case DataId.ExpressionChinRaise: sample.ExpressionChinRaise = new ItoH64(data).Float; break;
            case DataId.ExpressionLipPucker: sample.ExpressionLipPucker = new ItoH64(data).Float; break;
            case DataId.ExpressionLipPress: sample.ExpressionLipPress = new ItoH64(data).Float; break;
            case DataId.ExpressionLipSuck: sample.ExpressionLipSuck = new ItoH64(data).Float; break;
            case DataId.ExpressionMouthOpen: sample.ExpressionMouthOpen = new ItoH64(data).Float; break;
            case DataId.ExpressionSmirk: sample.ExpressionSmirk = new ItoH64(data).Float; break;
            case DataId.ExpressionAttention: sample.ExpressionAttention = new ItoH64(data).Float; break;
            case DataId.ExpressionEyeWiden: sample.ExpressionEyeWiden = new ItoH64(data).Float; break;
            case DataId.ExpressionCheekRaise: sample.ExpressionCheekRaise = new ItoH64(data).Float; break;
            case DataId.ExpressionLidTighten: sample.ExpressionLidTighten = new ItoH64(data).Float; break;
            case DataId.ExpressionDimpler: sample.ExpressionDimpler = new ItoH64(data).Float; break;
            case DataId.ExpressionLipStretch: sample.ExpressionLipStretch = new ItoH64(data).Float; break;
            case DataId.ExpressionJawDrop: sample.ExpressionJawDrop = new ItoH64(data).Float; break;
            case DataId.ExpressionQ: sample.ExpressionQ = new ItoH64(data).Float; break;

            default: throw new NotImplementedException();
        };
    }

    private static ushort[] DecodeVector(byte[] data)
    {
        var vectorSize = new ItoH16(data[..2]).UInt;
        Debug.Assert(data.Length == sizeof(ushort) * (1 + vectorSize));

        var wordSize = sizeof(ushort);
        var vector = new List<ushort>();
        for (int i = 1; i <= vectorSize; i++)
        {
            vector.Add(new ItoH16(data[(wordSize * i)..(wordSize * i + 1)]).UInt);
        }
        return vector.ToArray();
    }

    private static Point3D DecodePoint3D(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(Point3D)));
        return new Point3D()
        {
            X = new ItoH64(data[0..8]).Float,
            Y = new ItoH64(data[8..16]).Float,
            Z = new ItoH64(data[16..]).Float,
        };
    }

    private static Point2D DecodePoint2D(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(Point2D)));
        return new Point2D()
        {
            X = new ItoH64(data[0..8]).Float,
            Y = new ItoH64(data[8..]).Float,
        };
    }

    private static Vector3D DecodeVector3D(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(Vector3D)));
        return new Vector3D()
        {
            X = new ItoH64(data[0..8]).Float,
            Y = new ItoH64(data[8..16]).Float,
            Z = new ItoH64(data[16..]).Float,
        };
    }

    private static Quaternion DecodeQuaternion(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(Quaternion)));
        return new Quaternion()
        {
            W = new ItoH64(data[0..8]).Float,
            X = new ItoH64(data[8..16]).Float,
            Y = new ItoH64(data[16..24]).Float,
            Z = new ItoH64(data[24..]).Float,
        };
    }

    private static WorldIntersection[] DecodeWorldIntersectionList(byte[] data)
    {
        var count = new ItoH16(data[..2]).UInt;

        int point3DSize = Marshal.SizeOf(typeof(Point3D));
        var result = new List<WorldIntersection>();

        int offset = 2;
        for (int i = 0; i < count; i++)
        {
            var intersection = new WorldIntersection()
            {
                WorldPoint = DecodePoint3D(data[offset..(offset + point3DSize)]),
                ObjectPoint = DecodePoint3D(data[(offset + point3DSize)..(offset + 2 * point3DSize)]),
                ObjectName = DecodeString(data[(offset + 2 * point3DSize)..]),
            };
            result.Add(intersection);
            offset += intersection.StructSize;
        }

        Debug.Assert(data.Length == offset);

        return result.ToArray();
    }

    private static WorldIntersection? DecodeWorldIntersection(byte[] data)
    {
        var result = DecodeWorldIntersectionList(data);
        return result.Length > 0 ? result[0] : null;
    }


    private static String DecodeString(byte[] data)
    {
        var stringSize = new ItoH16(data[..2]).UInt;
        var result = new String()
        {
            Size = stringSize,
            Ptr = data[2..(2 + stringSize)].Select(b => (char)b).ToArray(),
        };
        Debug.Assert(data.Length == result.StructSize);
        return result;
    }

    private static UserMarker DecodeMarker(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(UserMarker)));
        return new UserMarker()
        {
            Error = new ItoH32(data[..4]).Int,
            CameraClock = new ItoH64(data[4..12]).UInt,
            CameraIdx = data[12],
            Data = new ItoH64(data[13..]).UInt,
        };
    }
}
