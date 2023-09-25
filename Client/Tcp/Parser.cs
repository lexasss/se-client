using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SEClient.Tcp;

public static class Parser
{
    public static SEPacketHeader ReadHeader(NetworkStream stream)
    {
        SEPacketHeader header = new() { SyncId = 0x44504553, PacketType = 4, Length = 0 };

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
                        Debug.WriteLine($"Received payload of {header.Length} bytes");
                    }

                    break;
                }
            }
            else
            {
                Debug.WriteLine($"non-header byte '{buffer[0]}', expected '{HEADER_START[offset]}'");
                offset = 0;
            }
        }

        return header;
    }

    public static SEOutputData ReadPayload(NetworkStream stream, ushort length)
    {
        var sample = new SEOutputData();

        int bytesRead = 0;
        int packets = 0;
        while (bytesRead < length)
        {
            int count = ReadSubHeader(stream, out SESubPacketHeader subHeader);
            bytesRead += count;
            if (count == 0)
                break;

            byte[] subPayload = new byte[subHeader.Length];
            count = stream.Read(subPayload, 0, subPayload.Length);
            bytesRead += count;
            if (count == 0)
                break;

            packets += 1;
            DecodeData(ref sample, subHeader.Id, subPayload);
        }

        if (bytesRead >= length)
        {
            Debug.WriteLine($"got '{packets}' packets");
        }
        else
        {
            Debug.WriteLine($"could not read packets");
        }

        return sample;
    }

    // Internal 

    static readonly char[] HEADER_START = new char[] { 'S', 'E', 'P', 'D', '\x00', '\x04' };

    private static int ReadSubHeader(NetworkStream stream, out SESubPacketHeader subHeader)
    {
        subHeader = new();

        byte[] buffer = new byte[16];
        int bytesRead = 0;
        int count;

        if ((count = stream.Read(buffer, 0, sizeof(ushort))) == 0)
            return 0;

        bytesRead += count;
        subHeader.Id = (SEOutputDataIds)new ItoH16(buffer).UInt;

        if ((count = stream.Read(buffer, 0, sizeof(ushort))) == 0)
            return 0;

        bytesRead += count;
        subHeader.Length = new ItoH16(buffer).UInt;

        return bytesRead;
    }

    private static void DecodeData(ref SEOutputData sample, SEOutputDataIds id, byte[] data)
    {
        switch (id)
        {
            case SEOutputDataIds.FrameNumber: sample.FrameNumber = new ItoH32(data).UInt; break;
            case SEOutputDataIds.EstimatedDelay: sample.EstimatedDelay = new ItoH32(data).UInt; break;
            case SEOutputDataIds.TimeStamp: sample.TimeStamp = new ItoH64(data).UInt; break;
            case SEOutputDataIds.UserTimeStamp: sample.UserTimeStamp = new ItoH64(data).UInt; break;
            case SEOutputDataIds.FrameRate: sample.FrameRate = new ItoH64(data).Float; break;
            case SEOutputDataIds.CameraPositions: sample.CameraPositions = DecodeVector(data); break;
            case SEOutputDataIds.CameraRotations: sample.CameraRotations = DecodeVector(data); break;
            case SEOutputDataIds.UserDefinedData: sample.UserDefinedData = new ItoH64(data).UInt; break;
            case SEOutputDataIds.RealTimeClock: sample.RealTimeClock = new ItoH64(data).UInt; break;
            case SEOutputDataIds.KeyboardState: sample.KeyboardState = DecodeString(data); break;
            case SEOutputDataIds.ASCIIKeyboardState: sample.ASCIIKeyboardState = new ItoH16(data).UInt; break;
            case SEOutputDataIds.UserMarker: sample.UserMarker = DecodeMarker(data); break;
            case SEOutputDataIds.CameraClocks: sample.CameraClocks = DecodeVector(data); break;

            //Head Position
            case SEOutputDataIds.HeadPosition: sample.HeadPosition = DecodePoint3D(data); break;
            case SEOutputDataIds.HeadPositionQ: sample.HeadPositionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.HeadRotationRodrigues: sample.HeadRotationRodrigues = DecodeVector3D(data); break;
            case SEOutputDataIds.HeadRotationQuaternion: sample.HeadRotationQuaternion = DecodeQuaternion(data); break;
            case SEOutputDataIds.HeadLeftEarDirection: sample.HeadLeftEarDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.HeadUpDirection: sample.HeadUpDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.HeadNoseDirection: sample.HeadNoseDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.HeadHeading: sample.HeadHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.HeadPitch: sample.HeadPitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.HeadRoll: sample.HeadRoll = new ItoH64(data).Float; break;
            case SEOutputDataIds.HeadRotationQ: sample.HeadRotationQ = new ItoH64(data).Float; break;

            //Raw Gaze
            case SEOutputDataIds.GazeOrigin: sample.GazeOrigin = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftGazeOrigin: sample.LeftGazeOrigin = DecodePoint3D(data); break;
            case SEOutputDataIds.RightGazeOrigin: sample.RightGazeOrigin = DecodePoint3D(data); break;
            case SEOutputDataIds.EyePosition: sample.EyePosition = DecodePoint3D(data); break;
            case SEOutputDataIds.GazeDirection: sample.GazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.GazeDirectionQ: sample.GazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftEyePosition: sample.LeftEyePosition = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftGazeDirection: sample.LeftGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.LeftGazeDirectionQ: sample.LeftGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightEyePosition: sample.RightEyePosition = DecodePoint3D(data); break;
            case SEOutputDataIds.RightGazeDirection: sample.RightGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.RightGazeDirectionQ: sample.RightGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.GazeHeading: sample.GazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.GazePitch: sample.GazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftGazeHeading: sample.LeftGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftGazePitch: sample.LeftGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightGazeHeading: sample.RightGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightGazePitch: sample.RightGazePitch = new ItoH64(data).Float; break;

            //Filtered Gaze
            case SEOutputDataIds.FilteredGazeDirection: sample.FilteredGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.FilteredGazeDirectionQ: sample.FilteredGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredLeftGazeDirection: sample.FilteredLeftGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.FilteredLeftGazeDirectionQ: sample.FilteredLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredRightGazeDirection: sample.FilteredRightGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.FilteredRightGazeDirectionQ: sample.FilteredRightGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredGazeHeading: sample.FilteredGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredGazePitch: sample.FilteredGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredLeftGazeHeading: sample.FilteredLeftGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredLeftGazePitch: sample.FilteredLeftGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredRightGazeHeading: sample.FilteredRightGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredRightGazePitch: sample.FilteredRightGazePitch = new ItoH64(data).Float; break;

            //Analysis (non-real-time)
            case SEOutputDataIds.Saccade: sample.Saccade = new ItoH32(data).UInt; break;
            case SEOutputDataIds.Fixation: sample.Fixation = new ItoH32(data).UInt; break;
            case SEOutputDataIds.Blink: sample.Blink = new ItoH32(data).UInt; break;
            case SEOutputDataIds.LeftBlinkClosingMidTime: sample.LeftBlinkClosingMidTime = new ItoH64(data).UInt; break;
            case SEOutputDataIds.LeftBlinkOpeningMidTime: sample.LeftBlinkOpeningMidTime = new ItoH64(data).UInt; break;
            case SEOutputDataIds.LeftBlinkClosingAmplitude: sample.LeftBlinkClosingAmplitude = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftBlinkOpeningAmplitude: sample.LeftBlinkOpeningAmplitude = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftBlinkClosingSpeed: sample.LeftBlinkClosingSpeed = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftBlinkOpeningSpeed: sample.LeftBlinkOpeningSpeed = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightBlinkClosingMidTime: sample.RightBlinkClosingMidTime = new ItoH64(data).UInt; break;
            case SEOutputDataIds.RightBlinkOpeningMidTime: sample.RightBlinkOpeningMidTime = new ItoH64(data).UInt; break;
            case SEOutputDataIds.RightBlinkClosingAmplitude: sample.RightBlinkClosingAmplitude = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightBlinkOpeningAmplitude: sample.RightBlinkOpeningAmplitude = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightBlinkClosingSpeed: sample.RightBlinkClosingSpeed = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightBlinkOpeningSpeed: sample.RightBlinkOpeningSpeed = new ItoH64(data).Float; break;

            //Intersections
            case SEOutputDataIds.ClosestWorldIntersection: sample.ClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.FilteredClosestWorldIntersection: sample.FilteredClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.AllWorldIntersections: sample.AllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.FilteredAllWorldIntersections: sample.FilteredAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.ZoneId: sample.ZoneId = new ItoH16(data).UInt; break;
            case SEOutputDataIds.EstimatedClosestWorldIntersection: sample.EstimatedClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.EstimatedAllWorldIntersections: sample.EstimatedAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.HeadClosestWorldIntersection: sample.HeadClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.HeadAllWorldIntersections: sample.HeadAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.CalibrationGazeIntersection: sample.CalibrationGazeIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.TaggedGazeIntersection: sample.TaggedGazeIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.LeftClosestWorldIntersection: sample.LeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.LeftAllWorldIntersections: sample.LeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.RightClosestWorldIntersection: sample.RightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.RightAllWorldIntersections: sample.RightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.FilteredLeftClosestWorldIntersection: sample.FilteredLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.FilteredLeftAllWorldIntersections: sample.FilteredLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.FilteredRightClosestWorldIntersection: sample.FilteredRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.FilteredRightAllWorldIntersections: sample.FilteredRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.EstimatedLeftClosestWorldIntersection: sample.EstimatedLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.EstimatedLeftAllWorldIntersections: sample.EstimatedLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.EstimatedRightClosestWorldIntersection: sample.EstimatedRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.EstimatedRightAllWorldIntersections: sample.EstimatedRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.FilteredEstimatedClosestWorldIntersection: sample.FilteredEstimatedClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.FilteredEstimatedAllWorldIntersections: sample.FilteredEstimatedAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.FilteredEstimatedLeftClosestWorldIntersection: sample.FilteredEstimatedLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.FilteredEstimatedLeftAllWorldIntersections: sample.FilteredEstimatedLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case SEOutputDataIds.FilteredEstimatedRightClosestWorldIntersection: sample.FilteredEstimatedRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case SEOutputDataIds.FilteredEstimatedRightAllWorldIntersections: sample.FilteredEstimatedRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;

            //Eyelid
            case SEOutputDataIds.EyelidOpening: sample.EyelidOpening = new ItoH64(data).Float; break;
            case SEOutputDataIds.EyelidOpeningQ: sample.EyelidOpeningQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftEyelidOpening: sample.LeftEyelidOpening = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftEyelidOpeningQ: sample.LeftEyelidOpeningQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightEyelidOpening: sample.RightEyelidOpening = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightEyelidOpeningQ: sample.RightEyelidOpeningQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftLowerEyelidExtremePoint: sample.LeftLowerEyelidExtremePoint = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftUpperEyelidExtremePoint: sample.LeftUpperEyelidExtremePoint = DecodePoint3D(data); break;
            case SEOutputDataIds.RightLowerEyelidExtremePoint: sample.RightLowerEyelidExtremePoint = DecodePoint3D(data); break;
            case SEOutputDataIds.RightUpperEyelidExtremePoint: sample.RightUpperEyelidExtremePoint = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftEyelidState: sample.LeftEyelidState = data[0]; break;
            case SEOutputDataIds.RightEyelidState: sample.RightEyelidState = data[0]; break;

            //Pupilometry
            case SEOutputDataIds.PupilDiameter: sample.PupilDiameter = new ItoH64(data).Float; break;
            case SEOutputDataIds.PupilDiameterQ: sample.PupilDiameterQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftPupilDiameter: sample.LeftPupilDiameter = new ItoH64(data).Float; break;
            case SEOutputDataIds.LeftPupilDiameterQ: sample.LeftPupilDiameterQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightPupilDiameter: sample.RightPupilDiameter = new ItoH64(data).Float; break;
            case SEOutputDataIds.RightPupilDiameterQ: sample.RightPupilDiameterQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredPupilDiameter: sample.FilteredPupilDiameter = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredPupilDiameterQ: sample.FilteredPupilDiameterQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredLeftPupilDiameter: sample.FilteredLeftPupilDiameter = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredLeftPupilDiameterQ: sample.FilteredLeftPupilDiameterQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredRightPupilDiameter: sample.FilteredRightPupilDiameter = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredRightPupilDiameterQ: sample.FilteredRightPupilDiameterQ = new ItoH64(data).Float; break;

            //GPS Information
            case SEOutputDataIds.GPSPosition: sample.GPSPosition = DecodePoint2D(data); break;
            case SEOutputDataIds.GPSGroundSpeed: sample.GPSGroundSpeed = new ItoH64(data).Float; break;
            case SEOutputDataIds.GPSCourse: sample.GPSCourse = new ItoH64(data).Float; break;
            case SEOutputDataIds.GPSTime: sample.GPSTime = new ItoH64(data).UInt; break;

            //Raw Estimated Gaze
            case SEOutputDataIds.EstimatedGazeOrigin: sample.EstimatedGazeOrigin = DecodePoint3D(data); break;
            case SEOutputDataIds.EstimatedLeftGazeOrigin: sample.EstimatedLeftGazeOrigin = DecodePoint3D(data); break;
            case SEOutputDataIds.EstimatedRightGazeOrigin: sample.EstimatedRightGazeOrigin = DecodePoint3D(data); break;
            case SEOutputDataIds.EstimatedEyePosition: sample.EstimatedEyePosition = DecodePoint3D(data); break;
            case SEOutputDataIds.EstimatedGazeDirection: sample.EstimatedGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.EstimatedGazeDirectionQ: sample.EstimatedGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedGazeHeading: sample.EstimatedGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedGazePitch: sample.EstimatedGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedLeftEyePosition: sample.EstimatedLeftEyePosition = DecodePoint3D(data); break;
            case SEOutputDataIds.EstimatedLeftGazeDirection: sample.EstimatedLeftGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.EstimatedLeftGazeDirectionQ: sample.EstimatedLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedLeftGazeHeading: sample.EstimatedLeftGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedLeftGazePitch: sample.EstimatedLeftGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedRightEyePosition: sample.EstimatedRightEyePosition = DecodePoint3D(data); break;
            case SEOutputDataIds.EstimatedRightGazeDirection: sample.EstimatedRightGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.EstimatedRightGazeDirectionQ: sample.EstimatedRightGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedRightGazeHeading: sample.EstimatedRightGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.EstimatedRightGazePitch: sample.EstimatedRightGazePitch = new ItoH64(data).Float; break;

            //Filtered Estimated Gaze
            case SEOutputDataIds.FilteredEstimatedGazeDirection: sample.FilteredEstimatedGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.FilteredEstimatedGazeDirectionQ: sample.FilteredEstimatedGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedGazeHeading: sample.FilteredEstimatedGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedGazePitch: sample.FilteredEstimatedGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedLeftGazeDirection: sample.FilteredEstimatedLeftGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.FilteredEstimatedLeftGazeDirectionQ: sample.FilteredEstimatedLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedLeftGazeHeading: sample.FilteredEstimatedLeftGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedLeftGazePitch: sample.FilteredEstimatedLeftGazePitch = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedRightGazeDirection: sample.FilteredEstimatedRightGazeDirection = DecodeVector3D(data); break;
            case SEOutputDataIds.FilteredEstimatedRightGazeDirectionQ: sample.FilteredEstimatedRightGazeDirectionQ = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedRightGazeHeading: sample.FilteredEstimatedRightGazeHeading = new ItoH64(data).Float; break;
            case SEOutputDataIds.FilteredEstimatedRightGazePitch: sample.FilteredEstimatedRightGazePitch = new ItoH64(data).Float; break;

            //Status
            case SEOutputDataIds.TrackingState: sample.TrackingState = data[0]; break;
            case SEOutputDataIds.EyeglassesStatus: sample.EyeglassesStatus = data[0]; break;
            case SEOutputDataIds.ReflexReductionStateDEPRECATED: sample.ReflexReductionStateDEPRECATED = data[0]; break;

            //Facial Feature Positions
            case SEOutputDataIds.LeftEyeOuterCorner3D: sample.LeftEyeOuterCorner3D = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftEyeInnerCorner3D: sample.LeftEyeInnerCorner3D = DecodePoint3D(data); break;
            case SEOutputDataIds.RightEyeInnerCorner3D: sample.RightEyeInnerCorner3D = DecodePoint3D(data); break;
            case SEOutputDataIds.RightEyeOuterCorner3D: sample.RightEyeOuterCorner3D = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftNostril3D: sample.LeftNostril3D = DecodePoint3D(data); break;
            case SEOutputDataIds.RightNostril3D: sample.RightNostril3D = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftMouthCorner3D: sample.LeftMouthCorner3D = DecodePoint3D(data); break;
            case SEOutputDataIds.RightMouthCorner3D: sample.RightMouthCorner3D = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftEar3D: sample.LeftEar3D = DecodePoint3D(data); break;
            case SEOutputDataIds.RightEar3D: sample.RightEar3D = DecodePoint3D(data); break;
            case SEOutputDataIds.NoseTip3D: sample.NoseTip3D = DecodePoint3D(data); break;
            case SEOutputDataIds.LeftEyeOuterCorner2D: sample.LeftEyeOuterCorner2D = DecodeVector(data); break;
            case SEOutputDataIds.LeftEyeInnerCorner2D: sample.LeftEyeInnerCorner2D = DecodeVector(data); break;
            case SEOutputDataIds.RightEyeInnerCorner2D: sample.RightEyeInnerCorner2D = DecodeVector(data); break;
            case SEOutputDataIds.RightEyeOuterCorner2D: sample.RightEyeOuterCorner2D = DecodeVector(data); break;
            case SEOutputDataIds.LeftNostril2D: sample.LeftNostril2D = DecodeVector(data); break;
            case SEOutputDataIds.RightNostril2D: sample.RightNostril2D = DecodeVector(data); break;
            case SEOutputDataIds.LeftMouthCorner2D: sample.LeftMouthCorner2D = DecodeVector(data); break;
            case SEOutputDataIds.RightMouthCorner2D: sample.RightMouthCorner2D = DecodeVector(data); break;
            case SEOutputDataIds.LeftEar2D: sample.LeftEar2D = DecodeVector(data); break;
            case SEOutputDataIds.RightEar2D: sample.RightEar2D = DecodeVector(data); break;
            case SEOutputDataIds.NoseTip2D: sample.NoseTip2D = DecodeVector(data); break;

            //Emotion
            case SEOutputDataIds.EmotionJoy: sample.EmotionJoy = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionFear: sample.EmotionFear = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionDisgust: sample.EmotionDisgust = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionSadness: sample.EmotionSadness = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionSurprise: sample.EmotionSurprise = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionValence: sample.EmotionValence = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionEngagement: sample.EmotionEngagement = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionSentimentality: sample.EmotionSentimentality = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionConfusion: sample.EmotionConfusion = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionNeutral: sample.EmotionNeutral = new ItoH64(data).Float; break;
            case SEOutputDataIds.EmotionQ: sample.EmotionQ = new ItoH64(data).Float; break;

            //Expression
            case SEOutputDataIds.ExpressionSmile: sample.ExpressionSmile = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionInnerBrowRaise: sample.ExpressionInnerBrowRaise = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionBrowRaise: sample.ExpressionBrowRaise = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionBrowFurrow: sample.ExpressionBrowFurrow = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionNoseWrinkle: sample.ExpressionNoseWrinkle = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionUpperLipRaise: sample.ExpressionUpperLipRaise = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionLipCornerDepressor: sample.ExpressionLipCornerDepressor = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionChinRaise: sample.ExpressionChinRaise = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionLipPucker: sample.ExpressionLipPucker = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionLipPress: sample.ExpressionLipPress = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionLipSuck: sample.ExpressionLipSuck = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionMouthOpen: sample.ExpressionMouthOpen = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionSmirk: sample.ExpressionSmirk = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionAttention: sample.ExpressionAttention = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionEyeWiden: sample.ExpressionEyeWiden = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionCheekRaise: sample.ExpressionCheekRaise = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionLidTighten: sample.ExpressionLidTighten = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionDimpler: sample.ExpressionDimpler = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionLipStretch: sample.ExpressionLipStretch = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionJawDrop: sample.ExpressionJawDrop = new ItoH64(data).Float; break;
            case SEOutputDataIds.ExpressionQ: sample.ExpressionQ = new ItoH64(data).Float; break;

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

    private static SEPoint3D DecodePoint3D(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(SEPoint3D)));
        return new SEPoint3D()
        {
            X = new ItoH64(data[0..8]).Float,
            Y = new ItoH64(data[8..16]).Float,
            Z = new ItoH64(data[16..]).Float,
        };
    }

    private static SEPoint2D DecodePoint2D(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(SEPoint2D)));
        return new SEPoint2D()
        {
            X = new ItoH64(data[0..8]).Float,
            Y = new ItoH64(data[8..]).Float,
        };
    }

    private static SEVect3D DecodeVector3D(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(SEVect3D)));
        return new SEVect3D()
        {
            X = new ItoH64(data[0..8]).Float,
            Y = new ItoH64(data[8..16]).Float,
            Z = new ItoH64(data[16..]).Float,
        };
    }

    private static SEQuaternion DecodeQuaternion(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(SEQuaternion)));
        return new SEQuaternion()
        {
            W = new ItoH64(data[0..8]).Float,
            X = new ItoH64(data[8..16]).Float,
            Y = new ItoH64(data[16..24]).Float,
            Z = new ItoH64(data[24..]).Float,
        };
    }

    private static SEWorldIntersection[] DecodeWorldIntersectionList(byte[] data)
    {
        var count = new ItoH16(data[..2]).UInt;

        int point3DSize = Marshal.SizeOf(typeof(SEPoint3D));
        var result = new List<SEWorldIntersection>();

        int offset = 2;
        for (int i = 0; i < count; i++)
        {
            var intersection = new SEWorldIntersection()
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

    private static SEWorldIntersection? DecodeWorldIntersection(byte[] data)
    {
        var result = DecodeWorldIntersectionList(data);
        return result.Length > 0 ? result[0] : null;
    }


    private static SEString DecodeString(byte[] data)
    {
        var stringSize = new ItoH16(data[..2]).UInt;
        var result = new SEString()
        {
            Size = stringSize,
            Ptr = data[2..(2 + stringSize)].Select(b => (char)b).ToArray(),
        };
        Debug.Assert(data.Length == result.StructSize);
        return result;
    }

    private static SEUserMarker DecodeMarker(byte[] data)
    {
        Debug.Assert(data.Length == Marshal.SizeOf(typeof(SEUserMarker)));
        return new SEUserMarker()
        {
            Error = new ItoH32(data[..4]).Int,
            CameraClock = new ItoH64(data[4..12]).UInt,
            CameraIdx = data[12],
            Data = new ItoH64(data[13..]).UInt,
        };
    }
}
