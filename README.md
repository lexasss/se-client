# SEClient

TCP client for receving data from SmartEye gaze tracker

## Using direct TCP connection

The client connects to the SmartEye's TCP port directly.

Use the following code to get the data:

```c#
using var tcpClient = new SEClient.Tcp.Client();

tcpClient.Sample += (s, e) => { /* consume data */ }

Exception? ex;
if ((ex = await tcpClient.Connect("127.0.0.1", 5002)) == null) { /* ok */
else { /* could not connect */ }
```

## Using output of `SocketClient.exe` [DEPRECATED]

Copy `SocketClient.exe` file from SmartEye's folder to the folder where you use `SEClient.dll`.
The data will be grabbed from its command-line output (the window is hidden).

Use the following code to get the data:

```c#
using SEClient.Cmd;

readonly Parser _parser = new ();
_parser.PlaneEnter += (s, e) => { /* consume data */};
_parser.PlaneExit += (s, e) => { /* consume data */};
_parser.Sample += (s, e) => { /* consume data */};

readonly DataSource _dataSource = new ();
_dataSource.Data += (s, e) => _parser.Feed(e);

_parser.Reset();
_dataSource.Start("127.0.0.1", 5002);
```
