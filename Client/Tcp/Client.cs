using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SEClient.Tcp;

public class Client : IDisposable
{
    public event EventHandler<Data>? Sample;
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

                var dataOrNull = Parser.ReadData(stream, header.Length);
                if (dataOrNull is Data data)
                {
                    data.Size = header.Length;
                    Sample?.Invoke(this, data);
                }
                else
                {
                    break;
                }

            } while (true);
        }
        catch { }

        stream.Dispose();

        Disconnected?.Invoke(this, new EventArgs());
    }
}
