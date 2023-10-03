Console.WriteLine($@"Usage:     .\TestTCP.exe [ip=127.0.0.1] [port=5002]");
Console.WriteLine($@"Example: > .\TestTCP.exe 192.168.1.132");
Console.WriteLine();

using var tcpClient = new SEClient.Tcp.Client();

/*
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
*/

tcpClient.Requested.Add(SEClient.Tcp.Data.Id.LeftPupilDiameter);
tcpClient.RequestAvailable += (s, e) =>
{
    object? pupilObj = e.GetValueOrDefault(SEClient.Tcp.Data.Id.LeftPupilDiameter);
    if (pupilObj is double pupil)
    {
        Console.WriteLine($"Pupil = {pupil}");
    }
};


// Parsing arguments

string ip = "127.0.0.1";
int port = 5002;

foreach (var arg in args)
{
    if (ushort.TryParse(arg, out ushort argPort))
    {
        port = argPort;
    }
    else if (arg.Split('.').Length == 4 && arg.Split('.').All(b => uint.TryParse(b, out uint val) && val < 255))
    {
        ip = arg;
    }
    else
    {
        Console.WriteLine($"Invalid command-line parameter: {arg}");
    }
}


// Connecting

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

