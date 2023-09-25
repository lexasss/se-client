using var tcpClient = new SEClient.Tcp.Client();

tcpClient.Sample += (s, e) =>
{
    if (e.LeftPupilDiameter is not null)
    {
        Console.WriteLine($"Pupil = {e.LeftPupilDiameter}");
    }
    if (e.ClosestWorldIntersection is not null)
    {
        Console.WriteLine($"Plane = {e.ClosestWorldIntersection?.ObjectName.String}");
    }
};

Console.WriteLine("Connecting to the SmartEye . . .");

Exception? ex;
if ((ex = await tcpClient.Connect("192.168.1.239", 5002)) == null)
{
    Console.WriteLine("Connected!");
    Console.WriteLine("Press Enter to exit");
    Console.ReadLine();
}
else
{
    Console.WriteLine(ex.Message);
}

