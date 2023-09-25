Console.WriteLine($@"Usage: .\Test.exe [ip=127.0.0.1] [port=5002]");
Console.WriteLine($@"   example: > .\Test.exe 192.168.1.132");
Console.WriteLine();

using var tcpClient = new SEClient.Tcp.Client();

tcpClient.Sample += (s, e) =>
{
    Console.WriteLine($"Received data of {e.Size} bytes");

    if (e.LeftPupilDiameter is not null)
    {
        Console.WriteLine($"Pupil = {e.LeftPupilDiameter}");
    }
    if (e.ClosestWorldIntersection is not null)
    {
        Console.WriteLine($"Plane = {e.ClosestWorldIntersection?.ObjectName.AsString}");
    }
};

// Parsing arguments

string ip = "127.0.0.1";
int port = 5002;

void HandleArg(string arg)
{
    if (ushort.TryParse(arg, out ushort argPort))
    {
        port = argPort;
    }
    else if (arg.Split('.').Length == 4 && arg.Split('.').All(p => uint.TryParse(p, out uint val) && val < 255))
    {
            ip = arg;
    }
    else
    {
        Console.WriteLine($"Invalid command-line parameter: {arg}");
    }
}

if (args.Length > 0)
    HandleArg(args[0]);
if (args.Length > 1)
    HandleArg(args[1]);

// Conectring

Console.WriteLine($"Connecting to the SmartEye on {ip}:{port} . . .");

Exception? ex;
if ((ex = await tcpClient.Connect(ip, port)) == null)
{
    Console.WriteLine("Connected!");
    Console.WriteLine("Press Enter to exit");
    Console.ReadLine();
}
else
{
    Console.WriteLine(ex.Message);
}

