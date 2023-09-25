using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SEClient.Tcp;

public class Client : IDisposable
{
    public event EventHandler<SEOutputData>? Sample;

    public Client()
    {
        _client = new TcpClient();
    }

    public async Task<Exception?> Connect(string ip, int port)
    {
        try
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(3000);

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
            return new Exception("Timeout");
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
        NetworkStream stream = _client.GetStream();

        try
        {
            do
            {
                SEPacketHeader header = Parser.ReadHeader(stream);
                if (header.Length == 0)
                    break;

                var payload = Parser.ReadPayload(stream, header.Length);
                Sample?.Invoke(this, payload);

            } while (true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: {0}", ex.Message);
        }
    }
}
