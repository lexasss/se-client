using System.Collections.Generic;

namespace SEClient.Cmd;

public enum Eye
{
    Left = 0,
    Right = 1
}

public struct PupilSize
{
    public double Quality;
    public double Diameter;
}

public struct EyeFeature
{
    public PupilSize Size;
}

public struct Intersection
{
    public int ID;
    public string PlaneName;
    public Point3D Gaze;
    public Point2D Point;
}

public struct Sample
{
    public int ID;
    public long TimeStamp;
    public double GazeDirectionQuality;
    public EyeFeature[] EyeFeature;     // 2 elements, left and right
    public List<Intersection> Intersections;
}
