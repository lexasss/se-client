using System;
using System.IO;
using System.Text.Json;

namespace SEClient;

public enum IntersectionSource
{
    Gaze,
    AI,
}

/// <summary>
/// Stores all options
/// </summary>
public class Options
{
    public static Options Instance => _instance ??= new ();

    public event EventHandler? Changed;

    // Parser

    public IntersectionSource IntersectionSource
    {
        get => _intersectionSource;
        set => Update(ref _intersectionSource, value);
    }
    public bool IntersectionSourceFiltered
    {
        get => _intersectionSourceFiltered;
        set => Update(ref _intersectionSourceFiltered, value);
    }
    public bool UseGazeQualityMeasurement
    {
        get => _useGazeQualityMeasurement;
        set => Update(ref _useGazeQualityMeasurement, value);
    }
    public double GazeQualityThreshold
    {
        get => _gazeQualityThreshold;
        set => Update(ref _gazeQualityThreshold, value);
    }

    // Load/Save

    /// <summary>
    /// Loads the options from the JSON file. Must be called at the very beginning of the application
    /// </summary>
    /// <param name="filename">The file that stores the options</param>
    /// <returns>Options object instance</returns>
    public static Options Load(string filename)
    {
        if (File.Exists(filename))
        {
            using var reader = new StreamReader(filename);
            string json = reader.ReadToEnd();
            _instance = (Options?)JsonSerializer.Deserialize(json, typeof(Options));
        }

        return Instance;
    }

    /// <summary>
    /// Saves the options to a JSON file 
    /// </summary>
    /// <param name="filename">The file to store the options</param>
    /// <exception cref="Exception">Throws is <see cref="Options"/> instance is not created yet</exception>
    public static void Save(string filename)
    {
        if (_instance == null)
        {
            throw new Exception("Options do not exist");
        }

        string json = JsonSerializer.Serialize(_instance);
        using var writer = new StreamWriter(filename);
        writer.Write(json);
    }

    // Internal

    static Options? _instance = null;

    IntersectionSource _intersectionSource = IntersectionSource.Gaze;
    bool _intersectionSourceFiltered = false;
    bool _useGazeQualityMeasurement = false;
    double _gazeQualityThreshold = 0.5;

    private void Update<T>(ref T member, T value)
    {
        member = value;
        Changed?.Invoke(this, new EventArgs());
    }
}
