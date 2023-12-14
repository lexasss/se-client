using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;

namespace SEClient.Tcp;

public class Client : IDisposable
{
    public HashSet<Data.Id> Requested { get; } = new();

    public bool IsConnected { get; private set; } = false;

    public bool IsEmulated { get; private set; } = false;

    /// <summary>
    /// This event will not fire if a handler to the <see cref="RequestAvailable"/> event is assigned already
    /// and <see cref=" Requested"/> set has at least one ID
    /// </summary>
    public event EventHandler<Data.Sample>? Sample;
    /// <summary>
    /// This event fires only if all data requested in  <see cref="Requested"/> is available
    /// </summary>
    public event EventHandler<Dictionary<Data.Id, object>>? RequestAvailable;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public Client()
    {
        _client = new TcpClient();
    }

    public async Task<Exception?> Connect(string ip, int port, bool isDebugging = false, int timeout = 3000)
    {
        IsEmulated = isDebugging;

        if (IsEmulated)
        {
            IsConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);
            Task.Run(Emulate, _emulatorCancellationSource.Token);
            return null;
        }

        if (_readingThread is not null)
            return new Exception($"The client is connected already");

        try
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            await _client.ConnectAsync(ip, port, cts.Token);
            IsConnected = true;

            _readingThread = new Thread(ReadInLoop);
            _readingThread.Start();
        }
        catch (SocketException ex)
        {
            return ex;
        }
        catch (OperationCanceledException)
        {
            return new TimeoutException($"Timeout in {timeout} ms.");
        }

        return null;
    }

    public async Task Stop()
    {
        IsConnected = false;

        if (IsEmulated)
        {
            _emulatorCancellationSource.Cancel();
            _emulatorCancellationSource = new();

            Disconnected?.Invoke(this, EventArgs.Empty);
            return;
        }

        await Task.Run(() => _readingThread?.Join());
    }

    public static void SetEmulatedPlanes(string[] planeNames)
    {
        var planes = new List<String>();
        foreach (var planName in planeNames)
        {
            planes.Add(new String() { Ptr = planName.Split().Select(s => s[0]).ToArray(), Size = (ushort)planName.Length });
        }
        EmulatedPlanes = planes.ToArray();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    // Internal

    static String[] EmulatedPlanes = new String[]
    {
            new String() { Ptr = new char[] { 'W', 'i', 'n', 'd', 's', 'h', 'i', 'e', 'l', 'd' }, Size = 10 },
            new String() { Ptr = new char[] { 'L', 'e', 'f', 't', 'M', 'i', 'r', 'r', 'o', 'r' }, Size = 10 },
            new String() { Ptr = new char[] { 'L', 'e', 'f', 't', 'D', 'a', 's', 'h', 'b', 'o', 'a', 'r', 'd' }, Size = 13 },
            new String() { Ptr = new char[] { 'C', 'e', 'n', 't', 'r', 'a', 'l', 'C', 'o', 'n', 's', 'o', 'l', 'e' }, Size = 14 },
            new String() { Ptr = new char[] { 'R', 'e', 'a', 'r', 'V', 'i', 'e', 'w' }, Size = 8 },
            new String() { Ptr = new char[] { 'R', 'i', 'g', 'h', 't', 'M', 'i', 'r', 'r', 'o', 'r' }, Size = 11 },
    };

    readonly TcpClient _client;

    Thread? _readingThread;
    CancellationTokenSource _emulatorCancellationSource = new();

    private void ReadInLoop()
    {
        Connected?.Invoke(this, new EventArgs());

        NetworkStream stream = _client.GetStream();

        try
        {
            do
            {
                PacketHeader header = Parser.ReadHeader(stream);
                if (header.Length == 0)
                {
                    break;
                }

                if (Requested.Count > 0 && RequestAvailable is not null)
                {
                    var dict = Parser.ReadDataRequested(stream, header.Length, Requested);
                    if (dict?.Count == Requested.Count)
                    {
                        RequestAvailable?.Invoke(this, dict);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (Sample is not null)
                {
                    var dataOrNull = Parser.ReadData(stream, header.Length);
                    if (dataOrNull is Data.Sample sample)
                    {
                        sample.Size = header.Length;
                        Sample?.Invoke(this, sample);
                    }
                    else
                    {
                        break;
                    }
                }
                else    // just read all data and forget immediately
                {
                    var buffer = new byte[header.Length];
                    stream.Read(buffer);
                }

            } while (IsConnected);
        }
        catch { }

        IsConnected = false;    // Set it if the thread was closed internally by error
        _readingThread = null;

        stream.Dispose();

        Disconnected?.Invoke(this, new EventArgs());
    }

    private async void Emulate()
    {
        var rnd = new Random();
        uint id = 0;
        String? currentPlane = null;

        Data.Sample GenerateSample()
        {
            if (currentPlane is not null)
            {
                currentPlane = null;
                return new Data.Sample()
                {
                    FrameNumber = id,
                    TimeStamp = (ulong)DateTime.Now.Ticks,
                };
            }
            else
            {
                currentPlane = EmulatedPlanes[rnd.Next(EmulatedPlanes.Length)];
                var intersection = new WorldIntersection()
                {
                    ObjectName = (String)currentPlane,
                    ObjectPoint = new Point3D() { X = rnd.NextDouble(), Y = rnd.NextDouble(), Z = 0 },
                    WorldPoint = new Point3D() { X = rnd.NextDouble(), Y = rnd.NextDouble(), Z = 0 },
                };
                return new Data.Sample()
                {
                    FrameNumber = id,
                    TimeStamp = (ulong)DateTime.Now.Ticks,
                    ClosestWorldIntersection = intersection,
                    AllWorldIntersections = new WorldIntersection[] { intersection },
                    FilteredClosestWorldIntersection = intersection,
                    FilteredAllWorldIntersections = new WorldIntersection[] { intersection },
                    EstimatedClosestWorldIntersection = intersection,
                    EstimatedAllWorldIntersections = new WorldIntersection[] { intersection },
                    FilteredEstimatedClosestWorldIntersection = intersection,
                    FilteredEstimatedAllWorldIntersections = new WorldIntersection[] { intersection }
                };
            }
        }

        while (true)
        {
            await Task.Delay((int)(100 + rnd.NextDouble() * 200));
            var sample = GenerateSample();  // gaze on a plane
            Sample?.Invoke(this, sample);

            await Task.Delay((int)(100 + rnd.NextDouble() * 5000));
            sample = GenerateSample();     // gaze off a plane
            Sample?.Invoke(this, sample);
        }
    }
}
