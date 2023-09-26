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

    public static Data.Sample? ReadData(NetworkStream stream, ushort length)
    {
        var data = new Data.Sample();

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

    public static Dictionary<Data.Id, object>? ReadDataRequested(NetworkStream stream, ushort length, HashSet<Data.Id> requests)
    {
        var dict = new Dictionary<Data.Id, object>();

        int bytesRead = 0;
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

            if (requests.Contains(subHeader.Id))
            {
                var obj = DecodeData(subHeader.Id, subPayload);
                if (obj is not null)
                {
                    dict.Add(subHeader.Id, obj);
                }
            }
        }

        return dict;
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
        subHeader.Id = (Data.Id)new ItoH16(buffer).UInt;

        if ((count = stream.Read(buffer, 0, sizeof(ushort))) == 0)
            return 0;

        bytesRead += count;
        subHeader.Length = new ItoH16(buffer).UInt;

        return bytesRead;
    }

    private static void DecodeData(ref Data.Sample sample, Data.Id id, byte[] data)
    {
        switch (id)
        {
            case Data.Id.FrameNumber: sample.FrameNumber = new ItoH32(data).UInt; break;
            case Data.Id.EstimatedDelay: sample.EstimatedDelay = new ItoH32(data).UInt; break;
            case Data.Id.TimeStamp: sample.TimeStamp = new ItoH64(data).UInt; break;
            case Data.Id.UserTimeStamp: sample.UserTimeStamp = new ItoH64(data).UInt; break;
            case Data.Id.FrameRate: sample.FrameRate = new ItoH64(data).Float; break;
            case Data.Id.CameraPositions: sample.CameraPositions = DecodeVector(data); break;
            case Data.Id.CameraRotations: sample.CameraRotations = DecodeVector(data); break;
            case Data.Id.UserDefinedData: sample.UserDefinedData = new ItoH64(data).UInt; break;
            case Data.Id.RealTimeClock: sample.RealTimeClock = new ItoH64(data).UInt; break;
            case Data.Id.KeyboardState: sample.KeyboardState = DecodeString(data); break;
            case Data.Id.ASCIIKeyboardState: sample.ASCIIKeyboardState = new ItoH16(data).UInt; break;
            case Data.Id.UserMarker: sample.UserMarker = DecodeMarker(data); break;
            case Data.Id.CameraClocks: sample.CameraClocks = DecodeVector(data); break;

            //Head Position
            case Data.Id.HeadPosition: sample.HeadPosition = DecodePoint3D(data); break;
            case Data.Id.HeadPositionQ: sample.HeadPositionQ = new ItoH64(data).Float; break;
            case Data.Id.HeadRotationRodrigues: sample.HeadRotationRodrigues = DecodeVector3D(data); break;
            case Data.Id.HeadRotationQuaternion: sample.HeadRotationQuaternion = DecodeQuaternion(data); break;
            case Data.Id.HeadLeftEarDirection: sample.HeadLeftEarDirection = DecodeVector3D(data); break;
            case Data.Id.HeadUpDirection: sample.HeadUpDirection = DecodeVector3D(data); break;
            case Data.Id.HeadNoseDirection: sample.HeadNoseDirection = DecodeVector3D(data); break;
            case Data.Id.HeadHeading: sample.HeadHeading = new ItoH64(data).Float; break;
            case Data.Id.HeadPitch: sample.HeadPitch = new ItoH64(data).Float; break;
            case Data.Id.HeadRoll: sample.HeadRoll = new ItoH64(data).Float; break;
            case Data.Id.HeadRotationQ: sample.HeadRotationQ = new ItoH64(data).Float; break;

            //Raw Gaze
            case Data.Id.GazeOrigin: sample.GazeOrigin = DecodePoint3D(data); break;
            case Data.Id.LeftGazeOrigin: sample.LeftGazeOrigin = DecodePoint3D(data); break;
            case Data.Id.RightGazeOrigin: sample.RightGazeOrigin = DecodePoint3D(data); break;
            case Data.Id.EyePosition: sample.EyePosition = DecodePoint3D(data); break;
            case Data.Id.GazeDirection: sample.GazeDirection = DecodeVector3D(data); break;
            case Data.Id.GazeDirectionQ: sample.GazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.LeftEyePosition: sample.LeftEyePosition = DecodePoint3D(data); break;
            case Data.Id.LeftGazeDirection: sample.LeftGazeDirection = DecodeVector3D(data); break;
            case Data.Id.LeftGazeDirectionQ: sample.LeftGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.RightEyePosition: sample.RightEyePosition = DecodePoint3D(data); break;
            case Data.Id.RightGazeDirection: sample.RightGazeDirection = DecodeVector3D(data); break;
            case Data.Id.RightGazeDirectionQ: sample.RightGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.GazeHeading: sample.GazeHeading = new ItoH64(data).Float; break;
            case Data.Id.GazePitch: sample.GazePitch = new ItoH64(data).Float; break;
            case Data.Id.LeftGazeHeading: sample.LeftGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.LeftGazePitch: sample.LeftGazePitch = new ItoH64(data).Float; break;
            case Data.Id.RightGazeHeading: sample.RightGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.RightGazePitch: sample.RightGazePitch = new ItoH64(data).Float; break;

            //Filtered Gaze
            case Data.Id.FilteredGazeDirection: sample.FilteredGazeDirection = DecodeVector3D(data); break;
            case Data.Id.FilteredGazeDirectionQ: sample.FilteredGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredLeftGazeDirection: sample.FilteredLeftGazeDirection = DecodeVector3D(data); break;
            case Data.Id.FilteredLeftGazeDirectionQ: sample.FilteredLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredRightGazeDirection: sample.FilteredRightGazeDirection = DecodeVector3D(data); break;
            case Data.Id.FilteredRightGazeDirectionQ: sample.FilteredRightGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredGazeHeading: sample.FilteredGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.FilteredGazePitch: sample.FilteredGazePitch = new ItoH64(data).Float; break;
            case Data.Id.FilteredLeftGazeHeading: sample.FilteredLeftGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.FilteredLeftGazePitch: sample.FilteredLeftGazePitch = new ItoH64(data).Float; break;
            case Data.Id.FilteredRightGazeHeading: sample.FilteredRightGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.FilteredRightGazePitch: sample.FilteredRightGazePitch = new ItoH64(data).Float; break;

            //Analysis (non-real-time)
            case Data.Id.Saccade: sample.Saccade = new ItoH32(data).UInt; break;
            case Data.Id.Fixation: sample.Fixation = new ItoH32(data).UInt; break;
            case Data.Id.Blink: sample.Blink = new ItoH32(data).UInt; break;
            case Data.Id.LeftBlinkClosingMidTime: sample.LeftBlinkClosingMidTime = new ItoH64(data).UInt; break;
            case Data.Id.LeftBlinkOpeningMidTime: sample.LeftBlinkOpeningMidTime = new ItoH64(data).UInt; break;
            case Data.Id.LeftBlinkClosingAmplitude: sample.LeftBlinkClosingAmplitude = new ItoH64(data).Float; break;
            case Data.Id.LeftBlinkOpeningAmplitude: sample.LeftBlinkOpeningAmplitude = new ItoH64(data).Float; break;
            case Data.Id.LeftBlinkClosingSpeed: sample.LeftBlinkClosingSpeed = new ItoH64(data).Float; break;
            case Data.Id.LeftBlinkOpeningSpeed: sample.LeftBlinkOpeningSpeed = new ItoH64(data).Float; break;
            case Data.Id.RightBlinkClosingMidTime: sample.RightBlinkClosingMidTime = new ItoH64(data).UInt; break;
            case Data.Id.RightBlinkOpeningMidTime: sample.RightBlinkOpeningMidTime = new ItoH64(data).UInt; break;
            case Data.Id.RightBlinkClosingAmplitude: sample.RightBlinkClosingAmplitude = new ItoH64(data).Float; break;
            case Data.Id.RightBlinkOpeningAmplitude: sample.RightBlinkOpeningAmplitude = new ItoH64(data).Float; break;
            case Data.Id.RightBlinkClosingSpeed: sample.RightBlinkClosingSpeed = new ItoH64(data).Float; break;
            case Data.Id.RightBlinkOpeningSpeed: sample.RightBlinkOpeningSpeed = new ItoH64(data).Float; break;

            //Intersections
            case Data.Id.ClosestWorldIntersection: sample.ClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.FilteredClosestWorldIntersection: sample.FilteredClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.AllWorldIntersections: sample.AllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.FilteredAllWorldIntersections: sample.FilteredAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.ZoneId: sample.ZoneId = new ItoH16(data).UInt; break;
            case Data.Id.EstimatedClosestWorldIntersection: sample.EstimatedClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.EstimatedAllWorldIntersections: sample.EstimatedAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.HeadClosestWorldIntersection: sample.HeadClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.HeadAllWorldIntersections: sample.HeadAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.CalibrationGazeIntersection: sample.CalibrationGazeIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.TaggedGazeIntersection: sample.TaggedGazeIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.LeftClosestWorldIntersection: sample.LeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.LeftAllWorldIntersections: sample.LeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.RightClosestWorldIntersection: sample.RightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.RightAllWorldIntersections: sample.RightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.FilteredLeftClosestWorldIntersection: sample.FilteredLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.FilteredLeftAllWorldIntersections: sample.FilteredLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.FilteredRightClosestWorldIntersection: sample.FilteredRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.FilteredRightAllWorldIntersections: sample.FilteredRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.EstimatedLeftClosestWorldIntersection: sample.EstimatedLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.EstimatedLeftAllWorldIntersections: sample.EstimatedLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.EstimatedRightClosestWorldIntersection: sample.EstimatedRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.EstimatedRightAllWorldIntersections: sample.EstimatedRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.FilteredEstimatedClosestWorldIntersection: sample.FilteredEstimatedClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.FilteredEstimatedAllWorldIntersections: sample.FilteredEstimatedAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.FilteredEstimatedLeftClosestWorldIntersection: sample.FilteredEstimatedLeftClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.FilteredEstimatedLeftAllWorldIntersections: sample.FilteredEstimatedLeftAllWorldIntersections = DecodeWorldIntersectionList(data); break;
            case Data.Id.FilteredEstimatedRightClosestWorldIntersection: sample.FilteredEstimatedRightClosestWorldIntersection = DecodeWorldIntersection(data); break;
            case Data.Id.FilteredEstimatedRightAllWorldIntersections: sample.FilteredEstimatedRightAllWorldIntersections = DecodeWorldIntersectionList(data); break;

            //Eyelid
            case Data.Id.EyelidOpening: sample.EyelidOpening = new ItoH64(data).Float; break;
            case Data.Id.EyelidOpeningQ: sample.EyelidOpeningQ = new ItoH64(data).Float; break;
            case Data.Id.LeftEyelidOpening: sample.LeftEyelidOpening = new ItoH64(data).Float; break;
            case Data.Id.LeftEyelidOpeningQ: sample.LeftEyelidOpeningQ = new ItoH64(data).Float; break;
            case Data.Id.RightEyelidOpening: sample.RightEyelidOpening = new ItoH64(data).Float; break;
            case Data.Id.RightEyelidOpeningQ: sample.RightEyelidOpeningQ = new ItoH64(data).Float; break;
            case Data.Id.LeftLowerEyelidExtremePoint: sample.LeftLowerEyelidExtremePoint = DecodePoint3D(data); break;
            case Data.Id.LeftUpperEyelidExtremePoint: sample.LeftUpperEyelidExtremePoint = DecodePoint3D(data); break;
            case Data.Id.RightLowerEyelidExtremePoint: sample.RightLowerEyelidExtremePoint = DecodePoint3D(data); break;
            case Data.Id.RightUpperEyelidExtremePoint: sample.RightUpperEyelidExtremePoint = DecodePoint3D(data); break;
            case Data.Id.LeftEyelidState: sample.LeftEyelidState = data[0]; break;
            case Data.Id.RightEyelidState: sample.RightEyelidState = data[0]; break;

            //Pupilometry
            case Data.Id.PupilDiameter: sample.PupilDiameter = new ItoH64(data).Float; break;
            case Data.Id.PupilDiameterQ: sample.PupilDiameterQ = new ItoH64(data).Float; break;
            case Data.Id.LeftPupilDiameter: sample.LeftPupilDiameter = new ItoH64(data).Float; break;
            case Data.Id.LeftPupilDiameterQ: sample.LeftPupilDiameterQ = new ItoH64(data).Float; break;
            case Data.Id.RightPupilDiameter: sample.RightPupilDiameter = new ItoH64(data).Float; break;
            case Data.Id.RightPupilDiameterQ: sample.RightPupilDiameterQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredPupilDiameter: sample.FilteredPupilDiameter = new ItoH64(data).Float; break;
            case Data.Id.FilteredPupilDiameterQ: sample.FilteredPupilDiameterQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredLeftPupilDiameter: sample.FilteredLeftPupilDiameter = new ItoH64(data).Float; break;
            case Data.Id.FilteredLeftPupilDiameterQ: sample.FilteredLeftPupilDiameterQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredRightPupilDiameter: sample.FilteredRightPupilDiameter = new ItoH64(data).Float; break;
            case Data.Id.FilteredRightPupilDiameterQ: sample.FilteredRightPupilDiameterQ = new ItoH64(data).Float; break;

            //GPS Information
            case Data.Id.GPSPosition: sample.GPSPosition = DecodePoint2D(data); break;
            case Data.Id.GPSGroundSpeed: sample.GPSGroundSpeed = new ItoH64(data).Float; break;
            case Data.Id.GPSCourse: sample.GPSCourse = new ItoH64(data).Float; break;
            case Data.Id.GPSTime: sample.GPSTime = new ItoH64(data).UInt; break;

            //Raw Estimated Gaze
            case Data.Id.EstimatedGazeOrigin: sample.EstimatedGazeOrigin = DecodePoint3D(data); break;
            case Data.Id.EstimatedLeftGazeOrigin: sample.EstimatedLeftGazeOrigin = DecodePoint3D(data); break;
            case Data.Id.EstimatedRightGazeOrigin: sample.EstimatedRightGazeOrigin = DecodePoint3D(data); break;
            case Data.Id.EstimatedEyePosition: sample.EstimatedEyePosition = DecodePoint3D(data); break;
            case Data.Id.EstimatedGazeDirection: sample.EstimatedGazeDirection = DecodeVector3D(data); break;
            case Data.Id.EstimatedGazeDirectionQ: sample.EstimatedGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.EstimatedGazeHeading: sample.EstimatedGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.EstimatedGazePitch: sample.EstimatedGazePitch = new ItoH64(data).Float; break;
            case Data.Id.EstimatedLeftEyePosition: sample.EstimatedLeftEyePosition = DecodePoint3D(data); break;
            case Data.Id.EstimatedLeftGazeDirection: sample.EstimatedLeftGazeDirection = DecodeVector3D(data); break;
            case Data.Id.EstimatedLeftGazeDirectionQ: sample.EstimatedLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.EstimatedLeftGazeHeading: sample.EstimatedLeftGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.EstimatedLeftGazePitch: sample.EstimatedLeftGazePitch = new ItoH64(data).Float; break;
            case Data.Id.EstimatedRightEyePosition: sample.EstimatedRightEyePosition = DecodePoint3D(data); break;
            case Data.Id.EstimatedRightGazeDirection: sample.EstimatedRightGazeDirection = DecodeVector3D(data); break;
            case Data.Id.EstimatedRightGazeDirectionQ: sample.EstimatedRightGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.EstimatedRightGazeHeading: sample.EstimatedRightGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.EstimatedRightGazePitch: sample.EstimatedRightGazePitch = new ItoH64(data).Float; break;

            //Filtered Estimated Gaze
            case Data.Id.FilteredEstimatedGazeDirection: sample.FilteredEstimatedGazeDirection = DecodeVector3D(data); break;
            case Data.Id.FilteredEstimatedGazeDirectionQ: sample.FilteredEstimatedGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedGazeHeading: sample.FilteredEstimatedGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedGazePitch: sample.FilteredEstimatedGazePitch = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedLeftGazeDirection: sample.FilteredEstimatedLeftGazeDirection = DecodeVector3D(data); break;
            case Data.Id.FilteredEstimatedLeftGazeDirectionQ: sample.FilteredEstimatedLeftGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedLeftGazeHeading: sample.FilteredEstimatedLeftGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedLeftGazePitch: sample.FilteredEstimatedLeftGazePitch = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedRightGazeDirection: sample.FilteredEstimatedRightGazeDirection = DecodeVector3D(data); break;
            case Data.Id.FilteredEstimatedRightGazeDirectionQ: sample.FilteredEstimatedRightGazeDirectionQ = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedRightGazeHeading: sample.FilteredEstimatedRightGazeHeading = new ItoH64(data).Float; break;
            case Data.Id.FilteredEstimatedRightGazePitch: sample.FilteredEstimatedRightGazePitch = new ItoH64(data).Float; break;

            //Status
            case Data.Id.TrackingState: sample.TrackingState = data[0]; break;
            case Data.Id.EyeglassesStatus: sample.EyeglassesStatus = data[0]; break;
            case Data.Id.ReflexReductionStateDEPRECATED: sample.ReflexReductionStateDEPRECATED = data[0]; break;

            //Facial Feature Positions
            case Data.Id.LeftEyeOuterCorner3D: sample.LeftEyeOuterCorner3D = DecodePoint3D(data); break;
            case Data.Id.LeftEyeInnerCorner3D: sample.LeftEyeInnerCorner3D = DecodePoint3D(data); break;
            case Data.Id.RightEyeInnerCorner3D: sample.RightEyeInnerCorner3D = DecodePoint3D(data); break;
            case Data.Id.RightEyeOuterCorner3D: sample.RightEyeOuterCorner3D = DecodePoint3D(data); break;
            case Data.Id.LeftNostril3D: sample.LeftNostril3D = DecodePoint3D(data); break;
            case Data.Id.RightNostril3D: sample.RightNostril3D = DecodePoint3D(data); break;
            case Data.Id.LeftMouthCorner3D: sample.LeftMouthCorner3D = DecodePoint3D(data); break;
            case Data.Id.RightMouthCorner3D: sample.RightMouthCorner3D = DecodePoint3D(data); break;
            case Data.Id.LeftEar3D: sample.LeftEar3D = DecodePoint3D(data); break;
            case Data.Id.RightEar3D: sample.RightEar3D = DecodePoint3D(data); break;
            case Data.Id.NoseTip3D: sample.NoseTip3D = DecodePoint3D(data); break;
            case Data.Id.LeftEyeOuterCorner2D: sample.LeftEyeOuterCorner2D = DecodeVector(data); break;
            case Data.Id.LeftEyeInnerCorner2D: sample.LeftEyeInnerCorner2D = DecodeVector(data); break;
            case Data.Id.RightEyeInnerCorner2D: sample.RightEyeInnerCorner2D = DecodeVector(data); break;
            case Data.Id.RightEyeOuterCorner2D: sample.RightEyeOuterCorner2D = DecodeVector(data); break;
            case Data.Id.LeftNostril2D: sample.LeftNostril2D = DecodeVector(data); break;
            case Data.Id.RightNostril2D: sample.RightNostril2D = DecodeVector(data); break;
            case Data.Id.LeftMouthCorner2D: sample.LeftMouthCorner2D = DecodeVector(data); break;
            case Data.Id.RightMouthCorner2D: sample.RightMouthCorner2D = DecodeVector(data); break;
            case Data.Id.LeftEar2D: sample.LeftEar2D = DecodeVector(data); break;
            case Data.Id.RightEar2D: sample.RightEar2D = DecodeVector(data); break;
            case Data.Id.NoseTip2D: sample.NoseTip2D = DecodeVector(data); break;

            //Emotion
            case Data.Id.EmotionJoy: sample.EmotionJoy = new ItoH64(data).Float; break;
            case Data.Id.EmotionFear: sample.EmotionFear = new ItoH64(data).Float; break;
            case Data.Id.EmotionDisgust: sample.EmotionDisgust = new ItoH64(data).Float; break;
            case Data.Id.EmotionSadness: sample.EmotionSadness = new ItoH64(data).Float; break;
            case Data.Id.EmotionSurprise: sample.EmotionSurprise = new ItoH64(data).Float; break;
            case Data.Id.EmotionValence: sample.EmotionValence = new ItoH64(data).Float; break;
            case Data.Id.EmotionEngagement: sample.EmotionEngagement = new ItoH64(data).Float; break;
            case Data.Id.EmotionSentimentality: sample.EmotionSentimentality = new ItoH64(data).Float; break;
            case Data.Id.EmotionConfusion: sample.EmotionConfusion = new ItoH64(data).Float; break;
            case Data.Id.EmotionNeutral: sample.EmotionNeutral = new ItoH64(data).Float; break;
            case Data.Id.EmotionQ: sample.EmotionQ = new ItoH64(data).Float; break;

            //Expression
            case Data.Id.ExpressionSmile: sample.ExpressionSmile = new ItoH64(data).Float; break;
            case Data.Id.ExpressionInnerBrowRaise: sample.ExpressionInnerBrowRaise = new ItoH64(data).Float; break;
            case Data.Id.ExpressionBrowRaise: sample.ExpressionBrowRaise = new ItoH64(data).Float; break;
            case Data.Id.ExpressionBrowFurrow: sample.ExpressionBrowFurrow = new ItoH64(data).Float; break;
            case Data.Id.ExpressionNoseWrinkle: sample.ExpressionNoseWrinkle = new ItoH64(data).Float; break;
            case Data.Id.ExpressionUpperLipRaise: sample.ExpressionUpperLipRaise = new ItoH64(data).Float; break;
            case Data.Id.ExpressionLipCornerDepressor: sample.ExpressionLipCornerDepressor = new ItoH64(data).Float; break;
            case Data.Id.ExpressionChinRaise: sample.ExpressionChinRaise = new ItoH64(data).Float; break;
            case Data.Id.ExpressionLipPucker: sample.ExpressionLipPucker = new ItoH64(data).Float; break;
            case Data.Id.ExpressionLipPress: sample.ExpressionLipPress = new ItoH64(data).Float; break;
            case Data.Id.ExpressionLipSuck: sample.ExpressionLipSuck = new ItoH64(data).Float; break;
            case Data.Id.ExpressionMouthOpen: sample.ExpressionMouthOpen = new ItoH64(data).Float; break;
            case Data.Id.ExpressionSmirk: sample.ExpressionSmirk = new ItoH64(data).Float; break;
            case Data.Id.ExpressionAttention: sample.ExpressionAttention = new ItoH64(data).Float; break;
            case Data.Id.ExpressionEyeWiden: sample.ExpressionEyeWiden = new ItoH64(data).Float; break;
            case Data.Id.ExpressionCheekRaise: sample.ExpressionCheekRaise = new ItoH64(data).Float; break;
            case Data.Id.ExpressionLidTighten: sample.ExpressionLidTighten = new ItoH64(data).Float; break;
            case Data.Id.ExpressionDimpler: sample.ExpressionDimpler = new ItoH64(data).Float; break;
            case Data.Id.ExpressionLipStretch: sample.ExpressionLipStretch = new ItoH64(data).Float; break;
            case Data.Id.ExpressionJawDrop: sample.ExpressionJawDrop = new ItoH64(data).Float; break;
            case Data.Id.ExpressionQ: sample.ExpressionQ = new ItoH64(data).Float; break;

            default: throw new NotImplementedException();
        };
    }

    private static object? DecodeData(Data.Id id, byte[] data)
    {
        return id switch
        {
            Data.Id.FrameNumber => new ItoH32(data).UInt,
            Data.Id.EstimatedDelay => new ItoH32(data).UInt,
            Data.Id.TimeStamp => new ItoH64(data).UInt,
            Data.Id.UserTimeStamp => new ItoH64(data).UInt,
            Data.Id.FrameRate => new ItoH64(data).Float,
            Data.Id.CameraPositions => DecodeVector(data),
            Data.Id.CameraRotations => DecodeVector(data),
            Data.Id.UserDefinedData => new ItoH64(data).UInt,
            Data.Id.RealTimeClock => new ItoH64(data).UInt,
            Data.Id.KeyboardState => DecodeString(data),
            Data.Id.ASCIIKeyboardState => new ItoH16(data).UInt,
            Data.Id.UserMarker => DecodeMarker(data),
            Data.Id.CameraClocks => DecodeVector(data),

            //Head Position
            Data.Id.HeadPosition => DecodePoint3D(data),
            Data.Id.HeadPositionQ => new ItoH64(data).Float,
            Data.Id.HeadRotationRodrigues => DecodeVector3D(data),
            Data.Id.HeadRotationQuaternion => DecodeQuaternion(data),
            Data.Id.HeadLeftEarDirection => DecodeVector3D(data),
            Data.Id.HeadUpDirection => DecodeVector3D(data),
            Data.Id.HeadNoseDirection => DecodeVector3D(data),
            Data.Id.HeadHeading => new ItoH64(data).Float,
            Data.Id.HeadPitch => new ItoH64(data).Float,
            Data.Id.HeadRoll => new ItoH64(data).Float,
            Data.Id.HeadRotationQ => new ItoH64(data).Float,

            //Raw Gaze
            Data.Id.GazeOrigin => DecodePoint3D(data),
            Data.Id.LeftGazeOrigin => DecodePoint3D(data),
            Data.Id.RightGazeOrigin => DecodePoint3D(data),
            Data.Id.EyePosition => DecodePoint3D(data),
            Data.Id.GazeDirection => DecodeVector3D(data),
            Data.Id.GazeDirectionQ => new ItoH64(data).Float,
            Data.Id.LeftEyePosition => DecodePoint3D(data),
            Data.Id.LeftGazeDirection => DecodeVector3D(data),
            Data.Id.LeftGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.RightEyePosition => DecodePoint3D(data),
            Data.Id.RightGazeDirection => DecodeVector3D(data),
            Data.Id.RightGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.GazeHeading => new ItoH64(data).Float,
            Data.Id.GazePitch => new ItoH64(data).Float,
            Data.Id.LeftGazeHeading => new ItoH64(data).Float,
            Data.Id.LeftGazePitch => new ItoH64(data).Float,
            Data.Id.RightGazeHeading => new ItoH64(data).Float,
            Data.Id.RightGazePitch => new ItoH64(data).Float,

            //Filtered Gaze
            Data.Id.FilteredGazeDirection => DecodeVector3D(data),
            Data.Id.FilteredGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.FilteredLeftGazeDirection => DecodeVector3D(data),
            Data.Id.FilteredLeftGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.FilteredRightGazeDirection => DecodeVector3D(data),
            Data.Id.FilteredRightGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.FilteredGazeHeading => new ItoH64(data).Float,
            Data.Id.FilteredGazePitch => new ItoH64(data).Float,
            Data.Id.FilteredLeftGazeHeading => new ItoH64(data).Float,
            Data.Id.FilteredLeftGazePitch => new ItoH64(data).Float,
            Data.Id.FilteredRightGazeHeading => new ItoH64(data).Float,
            Data.Id.FilteredRightGazePitch => new ItoH64(data).Float,

            //Analysis (non-real-time)
            Data.Id.Saccade => new ItoH32(data).UInt,
            Data.Id.Fixation => new ItoH32(data).UInt,
            Data.Id.Blink => new ItoH32(data).UInt,
            Data.Id.LeftBlinkClosingMidTime => new ItoH64(data).UInt,
            Data.Id.LeftBlinkOpeningMidTime => new ItoH64(data).UInt,
            Data.Id.LeftBlinkClosingAmplitude => new ItoH64(data).Float,
            Data.Id.LeftBlinkOpeningAmplitude => new ItoH64(data).Float,
            Data.Id.LeftBlinkClosingSpeed => new ItoH64(data).Float,
            Data.Id.LeftBlinkOpeningSpeed => new ItoH64(data).Float,
            Data.Id.RightBlinkClosingMidTime => new ItoH64(data).UInt,
            Data.Id.RightBlinkOpeningMidTime => new ItoH64(data).UInt,
            Data.Id.RightBlinkClosingAmplitude => new ItoH64(data).Float,
            Data.Id.RightBlinkOpeningAmplitude => new ItoH64(data).Float,
            Data.Id.RightBlinkClosingSpeed => new ItoH64(data).Float,
            Data.Id.RightBlinkOpeningSpeed => new ItoH64(data).Float,

            //Intersections
            Data.Id.ClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.FilteredClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.AllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.FilteredAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.ZoneId => new ItoH16(data).UInt,
            Data.Id.EstimatedClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.EstimatedAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.HeadClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.HeadAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.CalibrationGazeIntersection => DecodeWorldIntersection(data),
            Data.Id.TaggedGazeIntersection => DecodeWorldIntersection(data),
            Data.Id.LeftClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.LeftAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.RightClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.RightAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.FilteredLeftClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.FilteredLeftAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.FilteredRightClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.FilteredRightAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.EstimatedLeftClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.EstimatedLeftAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.EstimatedRightClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.EstimatedRightAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.FilteredEstimatedClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.FilteredEstimatedAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.FilteredEstimatedLeftClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.FilteredEstimatedLeftAllWorldIntersections => DecodeWorldIntersectionList(data),
            Data.Id.FilteredEstimatedRightClosestWorldIntersection => DecodeWorldIntersection(data),
            Data.Id.FilteredEstimatedRightAllWorldIntersections => DecodeWorldIntersectionList(data),

            //Eyelid
            Data.Id.EyelidOpening => new ItoH64(data).Float,
            Data.Id.EyelidOpeningQ => new ItoH64(data).Float,
            Data.Id.LeftEyelidOpening => new ItoH64(data).Float,
            Data.Id.LeftEyelidOpeningQ => new ItoH64(data).Float,
            Data.Id.RightEyelidOpening => new ItoH64(data).Float,
            Data.Id.RightEyelidOpeningQ => new ItoH64(data).Float,
            Data.Id.LeftLowerEyelidExtremePoint => DecodePoint3D(data),
            Data.Id.LeftUpperEyelidExtremePoint => DecodePoint3D(data),
            Data.Id.RightLowerEyelidExtremePoint => DecodePoint3D(data),
            Data.Id.RightUpperEyelidExtremePoint => DecodePoint3D(data),
            Data.Id.LeftEyelidState => data[0],
            Data.Id.RightEyelidState => data[0],

            //Pupilometry
            Data.Id.PupilDiameter => new ItoH64(data).Float,
            Data.Id.PupilDiameterQ => new ItoH64(data).Float,
            Data.Id.LeftPupilDiameter => new ItoH64(data).Float,
            Data.Id.LeftPupilDiameterQ => new ItoH64(data).Float,
            Data.Id.RightPupilDiameter => new ItoH64(data).Float,
            Data.Id.RightPupilDiameterQ => new ItoH64(data).Float,
            Data.Id.FilteredPupilDiameter => new ItoH64(data).Float,
            Data.Id.FilteredPupilDiameterQ => new ItoH64(data).Float,
            Data.Id.FilteredLeftPupilDiameter => new ItoH64(data).Float,
            Data.Id.FilteredLeftPupilDiameterQ => new ItoH64(data).Float,
            Data.Id.FilteredRightPupilDiameter => new ItoH64(data).Float,
            Data.Id.FilteredRightPupilDiameterQ => new ItoH64(data).Float,

            //GPS Information
            Data.Id.GPSPosition => DecodePoint2D(data),
            Data.Id.GPSGroundSpeed => new ItoH64(data).Float,
            Data.Id.GPSCourse => new ItoH64(data).Float,
            Data.Id.GPSTime => new ItoH64(data).UInt,

            //Raw Estimated Gaze
            Data.Id.EstimatedGazeOrigin => DecodePoint3D(data),
            Data.Id.EstimatedLeftGazeOrigin => DecodePoint3D(data),
            Data.Id.EstimatedRightGazeOrigin => DecodePoint3D(data),
            Data.Id.EstimatedEyePosition => DecodePoint3D(data),
            Data.Id.EstimatedGazeDirection => DecodeVector3D(data),
            Data.Id.EstimatedGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.EstimatedGazeHeading => new ItoH64(data).Float,
            Data.Id.EstimatedGazePitch => new ItoH64(data).Float,
            Data.Id.EstimatedLeftEyePosition => DecodePoint3D(data),
            Data.Id.EstimatedLeftGazeDirection => DecodeVector3D(data),
            Data.Id.EstimatedLeftGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.EstimatedLeftGazeHeading => new ItoH64(data).Float,
            Data.Id.EstimatedLeftGazePitch => new ItoH64(data).Float,
            Data.Id.EstimatedRightEyePosition => DecodePoint3D(data),
            Data.Id.EstimatedRightGazeDirection => DecodeVector3D(data),
            Data.Id.EstimatedRightGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.EstimatedRightGazeHeading => new ItoH64(data).Float,
            Data.Id.EstimatedRightGazePitch => new ItoH64(data).Float,

            //Filtered Estimated Gaze
            Data.Id.FilteredEstimatedGazeDirection => DecodeVector3D(data),
            Data.Id.FilteredEstimatedGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedGazeHeading => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedGazePitch => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedLeftGazeDirection => DecodeVector3D(data),
            Data.Id.FilteredEstimatedLeftGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedLeftGazeHeading => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedLeftGazePitch => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedRightGazeDirection => DecodeVector3D(data),
            Data.Id.FilteredEstimatedRightGazeDirectionQ => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedRightGazeHeading => new ItoH64(data).Float,
            Data.Id.FilteredEstimatedRightGazePitch => new ItoH64(data).Float,

            //Status
            Data.Id.TrackingState => data[0],
            Data.Id.EyeglassesStatus => data[0],
            Data.Id.ReflexReductionStateDEPRECATED => data[0],

            //Facial Feature Positions
            Data.Id.LeftEyeOuterCorner3D => DecodePoint3D(data),
            Data.Id.LeftEyeInnerCorner3D => DecodePoint3D(data),
            Data.Id.RightEyeInnerCorner3D => DecodePoint3D(data),
            Data.Id.RightEyeOuterCorner3D => DecodePoint3D(data),
            Data.Id.LeftNostril3D => DecodePoint3D(data),
            Data.Id.RightNostril3D => DecodePoint3D(data),
            Data.Id.LeftMouthCorner3D => DecodePoint3D(data),
            Data.Id.RightMouthCorner3D => DecodePoint3D(data),
            Data.Id.LeftEar3D => DecodePoint3D(data),
            Data.Id.RightEar3D => DecodePoint3D(data),
            Data.Id.NoseTip3D => DecodePoint3D(data),
            Data.Id.LeftEyeOuterCorner2D => DecodeVector(data),
            Data.Id.LeftEyeInnerCorner2D => DecodeVector(data),
            Data.Id.RightEyeInnerCorner2D => DecodeVector(data),
            Data.Id.RightEyeOuterCorner2D => DecodeVector(data),
            Data.Id.LeftNostril2D => DecodeVector(data),
            Data.Id.RightNostril2D => DecodeVector(data),
            Data.Id.LeftMouthCorner2D => DecodeVector(data),
            Data.Id.RightMouthCorner2D => DecodeVector(data),
            Data.Id.LeftEar2D => DecodeVector(data),
            Data.Id.RightEar2D => DecodeVector(data),
            Data.Id.NoseTip2D => DecodeVector(data),

            //Emotion
            Data.Id.EmotionJoy => new ItoH64(data).Float,
            Data.Id.EmotionFear => new ItoH64(data).Float,
            Data.Id.EmotionDisgust => new ItoH64(data).Float,
            Data.Id.EmotionSadness => new ItoH64(data).Float,
            Data.Id.EmotionSurprise => new ItoH64(data).Float,
            Data.Id.EmotionValence => new ItoH64(data).Float,
            Data.Id.EmotionEngagement => new ItoH64(data).Float,
            Data.Id.EmotionSentimentality => new ItoH64(data).Float,
            Data.Id.EmotionConfusion => new ItoH64(data).Float,
            Data.Id.EmotionNeutral => new ItoH64(data).Float,
            Data.Id.EmotionQ => new ItoH64(data).Float,

            //Expression
            Data.Id.ExpressionSmile => new ItoH64(data).Float,
            Data.Id.ExpressionInnerBrowRaise => new ItoH64(data).Float,
            Data.Id.ExpressionBrowRaise => new ItoH64(data).Float,
            Data.Id.ExpressionBrowFurrow => new ItoH64(data).Float,
            Data.Id.ExpressionNoseWrinkle => new ItoH64(data).Float,
            Data.Id.ExpressionUpperLipRaise => new ItoH64(data).Float,
            Data.Id.ExpressionLipCornerDepressor => new ItoH64(data).Float,
            Data.Id.ExpressionChinRaise => new ItoH64(data).Float,
            Data.Id.ExpressionLipPucker => new ItoH64(data).Float,
            Data.Id.ExpressionLipPress => new ItoH64(data).Float,
            Data.Id.ExpressionLipSuck => new ItoH64(data).Float,
            Data.Id.ExpressionMouthOpen => new ItoH64(data).Float,
            Data.Id.ExpressionSmirk => new ItoH64(data).Float,
            Data.Id.ExpressionAttention => new ItoH64(data).Float,
            Data.Id.ExpressionEyeWiden => new ItoH64(data).Float,
            Data.Id.ExpressionCheekRaise => new ItoH64(data).Float,
            Data.Id.ExpressionLidTighten => new ItoH64(data).Float,
            Data.Id.ExpressionDimpler => new ItoH64(data).Float,
            Data.Id.ExpressionLipStretch => new ItoH64(data).Float,
            Data.Id.ExpressionJawDrop => new ItoH64(data).Float,
            Data.Id.ExpressionQ => new ItoH64(data).Float,

            _ => throw new Exception("Unknown data ID")
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
