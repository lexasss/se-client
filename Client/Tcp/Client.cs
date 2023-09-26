using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SEClient.Tcp;

public class Client : IDisposable
{
    public HashSet<Data.Id> Requested { get; } = new();

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

    public async Task<Exception?> Connect(string ip, int port, int timeout = 3000)
    {
        try
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            await _client.ConnectAsync(ip, port, cts.Token);

            var readingThread = new Thread(ReadInLoop);
            readingThread.Start();
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

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    // Internal

    readonly TcpClient _client;

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

            } while (true);
        }
        catch { }

        stream.Dispose();

        Disconnected?.Invoke(this, new EventArgs());
    }
}
