
# Documentation Server and Client - UDP
- UDP.Server
    - [UDP.Server.Start](#UDP.ServerStart)
    - [UDP.Server.Restart](#UDP.ServerRestart)
    - [UDP.Server.Stop](#UDP.ServerStop)
    - [UDP.Server.SendBytes](#UDP.ServerSendBytes)
    - [UDP.Server.SendBytesGroup](#SendBytesGroup)
    - [UDP.Server.SendBytesAll](#SendBytesAll)
    - [UDP.Server.DisconnectClient](#DisconnectClient)
    - [UDP.Server.DisconnectClientGroup](#DisconnectClientGroup)
    - [UDP.Server.DisconnectClientAll](#DisconnectClientAll)
    - [UDP.Server.ChangeLimitMaxByteSizeGroupID](#ChangeLimitMaxByteSizeGroupID)
    - [UDP.Server.ChangeLimitMaxPacketsPerSecondsGroupID](#ChangeLimitMaxPacketsPerSecondsGroupID)
    - [UDP.Server.ChangeBlockIP](#ChangeBlockIP)
    - [UDP.Server.Socket](#Socket)
    - [UDP.Server.LimitMaxByteReceive](#LimitMaxByteReceive)
    - [UDP.Server.LimitMaxPacketsPerSeconds](#LimitMaxPacketsPerSeconds)
    - [UDP.Server.Status](#Status)
    - [UDP.Server.LostPackets](#LostPackets)
    - [UDP.Server.MaxClients](#MaxClients)
    - [UDP.Server.PacketsPerSeconds](#PacketsPerSeconds)
    - [UDP.Server.PacketsBytesReceived](#PacketsBytesReceived)
    - [UDP.Server.PacketsBytesSent](#PacketsBytesSent)
    - [UDP.Server.ReceiveAndSendTimeOut](#ReceiveAndSendTimeOut)
    - [UDP.Server.ShowUnityNetworkStatistics](#UdpServerShowUnityNetworkStatistics)
    - [UDP.Server.ClientsCount](#ClientsCount)
    - [UDP.Server.ShowDebugConsole](#ShowDebugConsole)
    - [UDP.Server.OnConnectedClient](#OnConnectedClient)
    - [UDP.Server.OnDisconnectedClient](#OnDisconnectedClient)
    - [UDP.Server.ServerStatusConnection](#OnServerStatus)
    - [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient);
- UDP.Client
    - [UDP.Client.Connect](#UDP.ClientConnect)
    - [UDP.Client.SendBytes](#UDP.ClientSendBytes)
    - [UDP.Client.DisconnectServer](#DisconnectServer)
    - [UDP.Client.Socket](#Socket)
    - [UDP.Client.Status](#UDP.ClientStatus)
    - [UDP.Client.LostPackets](#UDP.ClientLostPackets)
    - [UDP.Client.PacketsPerSeconds](#UDP.ClientPacketsPerSeconds)
    - [UDP.Client.PacketsBytesReceived](#UDP.ClientPacketsBytesReceived)
    - [UDP.Client.PacketsBytesSent](#UDP.ClientPacketsBytesSent)
    - [UDP.Client.ReceiveAndSendTimeOut](#UDP.ClientReceiveAndSendTimeOut)
    - [UDP.Client.ConnectTimeOut](#ConnectTimeOut)
    - [UDP.Client.ShowUnityNetworkStatistics](#ShowUnityNetworkStatistics)
    - [UDP.Client.ShowDebugConsole](#ShowDebugConsole)
    - [UDP.Client.Ping](#Ping)
    - [UDP.Client.OnReceivedBytes](#OnReceivedBytesServer)
    - [UDP.Client.ClientStatusConnection](#OnClientStatus)
    - [UDP.Client.PublicKeyRSA](#PublicKeyRSA)
    - [UDP.Client.PrivateKeyAES](#PrivateKeyAES)
- Others
    - [SymmetricSizeRSA](#SymmetricSizeRSA)
    - [ServerStatusConnection](#ServerStatusConnection)
    - [ClientStatusConnection](#ClientStatusConnection)
    - [DataClient](#DataClient)
    - [TypeShipping](#TypeShipping)
    - [TypeHoldConnection](#TypeHoldConnection)
- FAQ
    - [GroupID](#GroupID)
    - [ShippingPreparation](#ShippingPreparation)

## UDP.Server

<a name="UDP.ServerStart"></a>
### UDP.Server.Start
`UDP.Server.Start(IPAddress _ip, int _port, int _symmetricSizeRSA = 86)`
```cs
UDP.Server.Start(IPAddress.Any, 25000, 16);
```
Start the server with specific IP, Port and sets the size of [SymmetricSizeRSA](#SymmetricSizeRSA) if needed. If the server has already been started and then stopped you can call ``UDP.Server.Start();`` without defining _host and _symmetricSizeRSA to start the server with the previous settings.

-----

<a name="UDP.ServerRestart"></a>
### UDP.Server.Restart
`UDP.Server.Restart()`
```cs
UDP.Server.Restart();
```
If the server is running, you can restart it, all connected clients will be disconnected from the server and new RSA and AES keys will be generated again.

-----

<a name="UDP.ServerStop"></a>
### UDP.Server.Stop
`UDP.Server.Stop()`
```cs
UDP.Server.Stop();
```
If the server is running, you can stop it, all connected clients will be disconnected from the server and if you start the server again new RSA and AES keys will be generated.

-----

<a name="UDP.ServerSendBytes"></a>
### UDP.Server.SendBytes
`UDP.Server.SendBytes(byte[] _byte, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, bool _holdConnection = false)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
   // Sending only groupID to client, without encryption and without HoldConnection
   UDP.Server.SendBytes(null, 4, _dataClient);

   var _bytes = System.Text.Encoding.ASCII.GetBytes("Hello world!");

   // Sending bytes with groupID to client, without encryption and without HoldConnection
   UDP.Server.SendBytes(_byte, 4, _dataClient);

   // Sending bytes with groupID to client, with encryption AES and without HoldConnection
   UDP.Server.SendBytes(_byte, 4, _dataClient, TypeShipping.AES);

   // Sending bytes with groupID to client, with encryption RSA and with HoldConnection
   UDP.Server.SendBytes(_byte, 4, _dataClient, TypeShipping.RSA, true);
}
```
To send bytes to a client, it is necessary to define the bytes, [GroupID](#GroupID) and [DataClient](#DataClient), the other sending resources such as [TypeShipping](#TypeShipping) and [HoldConnection](#HoldConnection) are optional.

-----

<a name="SendBytesGroup"></a>
### UDP.Server.SendBytesGroup
`UDP.Server.SendBytesGroup(byte[] _byte, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeShipping _typeShipping = TypeShipping.None, DataClient _skipDataClient = null, bool _holdConnection = false)`
```cs
using System.Collections.Concurrent;
static ConcurrentQueue<DataClient> PlayersLobby = new ConcurrentQueue<DataClient>();
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
   // Sending only groupID to all clients on the list, without encryption and without HoldConnection
   UDP.Server.SendBytesGroup(null, 4, _dataClient);

   var _bytes = System.Text.Encoding.ASCII.GetBytes("Play Game");

   // Sending bytes with groupID to all clients on the list
   UDP.Server.SendBytesGroup(_byte, 4, PlayersLobby);

   // Sending bytes with groupID to all clients on the list, except for the sending client.
   UDP.Server.SendBytesGroup(_byte, 4, PlayersLobby, _skipDataClient: _dataClient);
}
```
To send bytes to a group client, it is necessary to define the bytes, [GroupID](#GroupID) and [List DataClient](#DataClient), the other sending resources such as [TypeShipping](#TypeShipping), SkipDataClient and [HoldConnection](#HoldConnection) are optional.

-----

<a name="SendBytesAll"></a>
### UDP.Server.SendBytesAll
`UDP.Server.SendBytesAll(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, DataClient _skipDataClient = null, bool _holdConnection = false)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
   // Sending only groupID to all clients connected, without encryption and without HoldConnection
   UDP.Server.SendBytesAll(null, 4, _dataClient);

   var _bytes = System.Text.Encoding.ASCII.GetBytes("Play Game");

   // Sending bytes with groupID to all clients connected.
   UDP.Server.SendBytesAll(_byte, 4);

   // Sending bytes with groupID to all clients connected, except for the sending client.
   UDP.Server.SendBytesAll(_byte, 4, PlayersLobby, _skipDataClient: _dataClient);
}
```
To send bytes to all clients, it is necessary to define the bytes, [GroupID](#GroupID), the other sending resources such as [TypeShipping](#TypeShipping), SkipDataClient and [HoldConnection](#HoldConnection) are optional.

-----

<a name="DisconnectClient"></a>
### UDP.Server.DisconnectClient
`UDP.Server.DisconnectClient(DataClient _dataClient)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  UDP.Server.DisconnectClient(_dataClient);
}
```
To disconnect a client from server, it is necessary to inform the [DataClient](#DataClient).

-----

<a name="DisconnectClientGroup"></a>
### UDP.Server.DisconnectClientGroup
`UDP.Server.DisconnectClientGroup(<List>DataClient _dataClient)`
```cs
using System.Collections.Concurrent;
static ConcurrentQueue<DataClient> AFKPlayers = new ConcurrentQueue<DataClient>();
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  UDP.Server.DisconnectClientGroup(AFKPlayers);
}
```
To disconnect a group clients from server, it is necessary to inform the [List DataClient](#DataClient).

-----

<a name="DisconnectClientAll"></a>
### UDP.Server.DisconnectClientAll
`UDP.Server.DisconnectClientAll()`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  UDP.Server.DisconnectClientAll();
}
```
To disconnect alls clients from server.

-----

<a name="ChangeLimitMaxByteSizeGroupID"></a>
### UDP.Server.ChangeLimitMaxByteSizeGroupID
`UDP.Server.ChangeLimitMaxByteSizeGroupID(int _groupID, int _limitBytes)`
```cs
UDP.Server.ChangeLimitMaxByteSizeGroupID(4, 12);
```
The ChangeLimitMaxByteSizeGroupID will change the maximum limit of bytes of a [GroupID](#GroupID) that the server will read when receiving the bytes, if the packet bytes is greater than the limit, the server will not call the [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient) event with the received bytes. The default value _limitBytes is 0 which is unlimited.

-----

<a name="ChangeLimitMaxPacketsPerSecondsGroupID"></a>
### UDP.Server.ChangeLimitMaxPacketsPerSecondsGroupID
`UDP.Server.ChangeLimitMaxPacketsPerSecondsGroupID(int _groupID, int _limitPPS)`
```cs
UDP.Server.ChangeLimitMaxPacketsPerSecondsGroupID(4, 60);
```
The ChangeLimitMaxPacketsPerSecondsGroupID will change the maximum limit of Packets per seconds (PPS) of a [GroupID](#GroupID), if the packets is greater than the limit in 1 second, the server will not call the [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient) event with the received bytes. The default value is _limitBytes 0 which is unlimited.

-----

<a name="ChangeBlockIP"></a>
### UDP.Server.ChangeBlockIP
`UDP.Server.ChangeBlockIP(IPEndPoint _ip, int _time)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  // IP blocked per 1 hour
  UDP.Server.ChangeBlockIP(_dataClient.IP, 60000 * 60);
}
```
ChangeBlockIP blocks a specific IP for the time defined in milliseconds. If the time is 0 the IP will be removed from the server's blocked IP list.

-----

<a name="ClientsCount"></a>
### UDP.Server.ClientsCount
`Read-Only Variable`
```cs
static void OnConnectedClient(DataClient _dataClient){
    int onlines = UDP.Server.ClientsCount;
    Console.WriteLine("Has a total of {0} players connected.", onlines);
}
```
The ClientsCount is the total number of clients connected to the server.

-----

<a name="LimitMaxByteReceive"></a>
### UDP.Server.LimitMaxByteReceive
`Write/Read Variable`
```cs
// Limit in 12 bytes;
UDP.Server.LimitMaxByteReceive = 12;
```
The LimitMaxByteReceive will change the maximum limit of bytes that the server will read when receiving, if the packet bytes is greater than the limit, the server will not call the [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient) event with the received bytes. The default value is 0 which is unlimited.

-----

<a name="LimitMaxPacketsPerSeconds"></a>
### UDP.Server.LimitMaxPacketsPerSeconds
`Write/Read Variable`
```cs
// Limit in 60 pps;
UDP.Server.LimitMaxPacketsPerSeconds = 60;
```
The LimitMaxPacketsPerSeconds will change the maximum limit of Packets per seconds (PPS), if the packets is greater than the limit in 1 second, the server will not call the [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient) event with the received bytes. The default value is 0 which is unlimited.

-----

<a name="LostPackets"></a>
### UDP.Server.LostPackets
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  int LostPackets = UDP.Server.LostPackets;
  Console.WriteLine("{0} packets lost", LostPackets);
}
```
LostPackets is the number of packets lost.

-----

<a name="MaxClients"></a>
### UDP.Server.MaxClients
`Write/Read Variable`
```cs
UDP.Server.MaxClients = 32; // Maximum 32 Clients
```
MaxClients is the maximum number of clients that can connect to the server. If you have many connected clients and you change the value below the number of connected clients, they will not be disconnected, the server will block new connections until the number of connected clients is below or equal to the limit.

-----

<a name="PacketsPerSeconds"></a>
### UDP.Server.PacketsPerSeconds
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsPerSeconds = UDP.Server.PacketsPerSeconds;
  Console.WriteLine("{0} Packets Per Seconds", packetsPerSeconds);
}
```
PacketsPerSeconds is the amount of packets per second that happen when the server is sending and receiving.

-----

<a name="PacketsBytesReceived"></a>
### UDP.Server.PacketsBytesReceived
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesReceived = UDP.Server.PacketsBytesReceived;
  Console.WriteLine("Received: {0}", packetsBytesReceived);
}
```
PacketsBytesReceived is the amount of bytes received by the server.

-----

<a name="PacketsBytesSent"></a>
### UDP.Server.PacketsBytesSent
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesSent = UDP.Server.PacketsBytesSent;
  Console.WriteLine("Sent: {0}", packetsBytesSent);
}
```
PacketsBytesSent is the amount of bytes sent by the server.

-----

<a name="ReceiveAndSendTimeOut"></a>
### UDP.Server.ReceiveAndSendTimeOut
`Write/Read Variable`
```cs
UDP.Server.ReceiveAndSendTimeOut = 2000;
```
ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the server will ignore the receiving or sending. The default and recommended value is 1000.

-----

<a name="ShowDebugConsole"></a>
### UDP.Server.ShowDebugConsole or UDP.Client.ShowDebugConsole
`Write/Read Variable`
```cs
UDP.Server.ShowDebugConsole = false;
// Or
UDP.Client.ShowDebugConsole = false;
```
![Preview](/Images/DebugConsole.png)

The ShowDebugConsole when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. The default value is true.

-----

<a name="Socket"></a>
### UDP.Server.Socket or UDP.Client.Socket
`Write/Read Variable`

The Socket is a `System.Net.Sockets.UdpClient` variable. This is the main communication variable between Client and server.

-----

<a name="UdpServerShowUnityNetworkStatistics"></a>
### UdpServer.ShowUnityNetworkStatistics
`Write/Read Variable`
```cs
UdpServer.ShowUnityNetworkStatistics = true;
```

When using Nethostfire in Unity and when set the value of ShowUnityNetworkStatistics to true, statistics on server will be displayed in console running batchmode.

-----

<a name="Status"></a>
### UDP.Server.Status
`Write/Read Variable`
```cs
if(UDP.Server.Status == ServerStatusConnection.Running){
    // UDP.Server Running.
}
```
The Status is an enum [UDP.Server.ServerStatusConnection](#ServerStatusConnection) with it you can know the current state of the server.

-----

<a name="OnConnectedClient"></a>
### UDP.Server.OnConnectedClient
`Event`
```cs
static void Main(string[] args){
    UDP.Server.OnConnectedClient += OnConnectedClient;
    UDP.Server.Start(IPAddress.Any, 25000);
}

static void OnConnectedClient(DataClient _dataClient){
    Console.WriteLine(_dataClient.IP + " new client conected!");
}
```
OnConnectedClient is an event that you can use to receive the [DataClient](#DataClient) whenever a client connected.

-----

<a name="OnDisconnectedClient"></a>
### UDP.Server.OnDisconnectedClient
`Event`
```cs
static void Main(string[] args){
    UDP.Server.OnDisconnectedClient += OnDisconnectedClient;
    UDP.Server.Start(IPAddress.Any, 25000);
}

static void OnDisconnectedClient(DataClient _dataClient){
    Console.WriteLine(_dataClient.IP + " new client conected!");
}
```
OnDisconnectedClient is an event that you can use to receive the [DataClient](#DataClient) whenever a client disconnected.

-----

<a name="OnServerStatus"></a>
### UDP.Server.ServerStatusConnection
`Event`
```cs
static void Main(string[] args){
    UDP.Server.ServerStatusConnection += OnServerStatus;
    UDP.Server.Start(IPAddress.Any, 25000);
}

static void OnServerStatus(ServerStatusConnection _status){
    Console.WriteLine("UDP.Server Status: " + _status);
}
```
OnServerStatus is an event that returns [UDP.Server.ServerStatusConnection](#ServerStatusConnection) whenever the status changes, with which you can use it to know the current status of the server.

-----

<a name="OnReceivedBytesClient"></a>
### UDP.Server.OnReceivedBytes
`Event`
```cs
static void Main(string[] args){
    UDP.Server.OnReceivedBytes += OnReceivedBytesClient;
    UDP.Server.Start(IPAddress.Any, 25000);
}

static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
    Console.WriteLine("[RECEIVED] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
}
```
OnReceivedBytesClient an event that returns bytes received, [GroupID](#GroupID) and [DataClient](#DataClient) whenever the received bytes by clients, with it you can manipulate the bytes received.


## Client
<a name="UDP.ClientConnect"></a>
### UDP.Client.Connect
`Connect(IPEndPoint _host, int _symmetricSizeRSA = 86)`
```cs
UDP.Client.Connect(IPAddress.Parse("127.0.0.1"), 25000, 20);
```
Connect to a server with IP, Port and sets the size of [SymmetricSizeRSA](#SymmetricSizeRSA) if needed.

-----

<a name="UDP.ClientSendBytes"></a>
### UDP.Client.SendBytes
`SendBytes(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, bool _holdConnection = false)`
```cs
var _bytes = System.Text.Encoding.ASCII.GetBytes("Hello world!");

// Sending only groupID, without encryption and without HoldConnection
UDP.Client.SendBytes(null, 11);

// Sending bytes with groupID 4 to server, without encryption and without HoldConnection
UDP.Client.SendBytes(_byte, 4);

// Sending bytes with groupID 4 to server, with encryption AES and without HoldConnection
UDP.Client.SendBytes(_byte, 4, TypeShipping.AES);

// Sending bytes with groupID 4 to server, with encryption RSA and with HoldConnection
UDP.Client.SendBytes(_byte, 4, TypeShipping.RSA, true);
```
To send bytes to server, it is necessary to define the bytes and [GroupID](#GroupID), the other sending resources such as [TypeShipping](#TypeShipping) and [HoldConnection](#HoldConnection) are optional.

-----

<a name="DisconnectServer"></a>
### UDP.Client.DisconnectServer
`UDP.Client.DisconnectServer()`
```cs
UDP.Client.DisconnectServer();
```
With DisconnectServer the client will be disconnected from the server.

-----

<a name="UDP.ClientLostPackets"></a>
### UDP.Client.LostPackets
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  int LostPackets = UDP.Client.LostPackets;
  Console.WriteLine("{0} packets lost", LostPackets);
}
```
LostPackets is the number of packets lost.

-----

<a name="UDP.ClientPacketsPerSeconds"></a>
### UDP.Client.PacketsPerSeconds
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsPerSeconds = UDP.Client.PacketsPerSeconds;
  Console.WriteLine("{0} Packets Per Seconds", packetsPerSeconds);
}
```
PacketsPerSeconds is the amount of packets per second that happen when the client is sending and receiving.

-----

<a name="UDP.ClientPacketsBytesReceived"></a>
### UDP.Client.PacketsBytesReceived
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesReceived = UDP.Client.PacketsBytesReceived;
  Console.WriteLine("Received: {0}", packetsBytesReceived);
}
```
PacketsBytesReceived is the amount of bytes received by the client.

-----

<a name="UDP.ClientPacketsBytesSent"></a>
### UDP.Client.PacketsBytesSent
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesSent = UDP.Client.PacketsBytesSent;
  Console.WriteLine("Sent: {0}", packetsBytesSent);
}
```
PacketsBytesSent is the amount of bytes sent by the client.

-----

<a name="UDP.ClientReceiveAndSendTimeOut"></a>
### UDP.Client.ReceiveAndSendTimeOut
`Write/Read Variable`
```cs
UDP.Client.ReceiveAndSendTimeOut = 2000;
```
ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the client will ignore the receiving or sending. The default and recommended value is 1000.

-----

<a name="OnReceivedBytesServer"></a>
### UDP.Client.OnReceivedBytes
`Event`
```cs
static void Main(string[] args){
    UDP.Client.OnReceivedBytes += OnReceivedBytesServer;
    UDP.Client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
}

static void OnReceivedBytesServer(byte[] _byte, int _groupID){
    Console.WriteLine("[RECEIVED] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
}
```
OnReceivedBytesServer an event that returns bytes received and [GroupID](#GroupID) whenever the received bytes by clients, with it you can manipulate the bytes received.

-----

<a name="OnClientStatus"></a>
### UDP.Client.ClientStatusConnection
`Event`
```cs
static void Main(string[] args){
    UDP.Client.ClientStatusConnection += OnClientStatus;
    UDP.Server.Start(IPAddress.Any, 25000);
}

static void OnClientStatus(ClientStatusConnection _status){
    Console.WriteLine("Client Status: " + _status);
}
```
OnClientStatus is an event that returns [UDP.Client.ClientStatusConnection](#ClientStatusConnection) whenever the status changes, with which you can use it to know the current status of the server.

-----

<a name="Ping"></a>
### UDP.Client.Ping
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string ping = UDP.Client.Ping;
  Console.WriteLine("PING: {0}ms", ping);
}
```
Ping returns an integer value, this value is per milliseconds

-----

<a name="PublicKeyRSA"></a>
### UDP.Client.PublicKeyRSA
`Read-Only Variable`
```cs
static void OnClientStatus(ClientStatusConnection _status){
    if(_status == ClientStatusConnection.Connected){
        string publicKeyRSA = UDP.Client.PublicKeyRSA;
        Console.WriteLine("Public Key RSA: {0}", publicKeyRSA);
    }
}
```
PublicKeyRSA returns the RSA public key obtained by the server after connecting.

-----

<a name="PrivateKeyAES"></a>
### UDP.Client.PrivateKeyAES
`Read-Only Variable`
```cs
static void OnClientStatus(ClientStatusConnection _status){
    if(_status == ClientStatusConnection.Connected){
        string privateKeyAES = UDP.Client.PrivateKeyAES;
        Console.WriteLine("Private Key AES: {0}", privateKeyAES);
    }
}
```
PrivateKeyAES returns the AES private key obtained by the server after connecting.

-----

<a name="UDP.ClientStatus"></a>
### UDP.Client.Status
`Write/Read Variable`
```cs
if(UDP.Client.Status == ClientStatusConnection.Connected){
    // Client Connected.
}
```
The Status is an enum [UDP.Client.ClientStatusConnection](#ClientStatusConnection) with it you can know the current state of the client.

-----

<a name="ConnectTimeOut"></a>
### UDP.Client.ConnectTimeOut
`Write/Read Variable`
```cs
UDP.Client.ConnectTimeOut = 15000; // 15s
```
ConnectTimeOut is the time the client will be reconnecting with the server, the time is defined in milliseconds, if the value is 0 the client will be reconnecting infinitely. The default value is 10000.

-----

<a name="ShowUnityNetworkStatistics"></a>
### UDP.Client.ShowUnityNetworkStatistics
`Write/Read Variable`
```cs
UDP.Client.ShowUnityNetworkStatistics = true;
```

![Preview](/Images/NetworkStatistics.png)

When using Nethostfire in Unity and when set the value of ShowUnityNetworkStatistics to true, statistics on the connection between the client and the server will be displayed during game execution.

## Others

<a name="SymmetricSizeRSA"></a>
### SymmetricSizeRSA

SymmetricSizeRSA is the maximum size of bytes you can encrypt with RSA. Remember, the larger the Symmetric, the larger the RSA key, and the longer it will take to encrypt and decrypt. The default value is 86 bytes which represents 1024 bytes of RSA key.
| Symmetric Size (bytes) | RSA Key Size (bytes) | Encryption/Decryption Speed (ms)| Security |
|:---:|:---:|:---|:---:|
| 16  | 464 | 0,00682 Very Fast | Little Safe |
| 22 | 512  | 0,00870 Fast  | Moderate |
| 86 | 1024 | 0,01138 Moderate | Safe |
| 214 | 2048 | 0,01988 Slow | Very Safe |
| 470 | 4096 | 0,02692 Very Slow | Extremely safe |

_The minimum value of symmetricSizeRSA is 16 and the maximum value is 470. Encryption/Decryprion speed may vary depending on your machine's performance_

-----

<a name="DataClient"></a>
### DataClient
```cs
public class DataClient{
    public IPEndPoint IP; // IP Address
    public int PPS; // Packets per second
    public int Ping; // Ping (ms)
    public int Time; // Last time updated by the server.
    public int TimeLastPacket; // Last time received packet.
    public string PublicKeyRSA = null; // RSA key
    public byte[] PrivateKeyAES = null; // Private AES key
}
```
The DataClient class is used to store data from a client on the server. It is with this class that the server uses to define a client. The DataClients can be obtained with the following server events [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient), [UDP.Client.OnReceivedBytes](#OnReceivedBytesServer), [UDP.Server.OnConnectedClient](#OnConnectedClient) and [UDP.Server.OnDisconnectedClient](#OnDisconnectedClient)

-----

<a name="ServerStatusConnection"></a>
### UDP.Server.ServerStatusConnection
```cs
public enum ServerStatusConnection{
    Stopped = 0,
    Stopping = 1,
    Running = 2,
    Initializing = 3,
    Restarting = 4,
}
```
The ServerStatusConnection is used to define server states. The ServerStatusConnection can be obtained by the [UDP.Server.Status](#Status) variable or with the event [UDP.Server.ServerStatusConnection](#OnServerStatus)

-----

<a name="TypeShipping"></a>
### TypeShipping
```cs
public enum TypeShipping{
    None = 0,
    AES = 1,
    RSA = 2,
    Base64 = 3,
    Compress = 4,
    OnlyBase64 = 5,
    OnlyCompress = 6,
}
```
The TypeShipping is used to define the type of encryption of the bytes when being sent, Encryptions are automatically decrypted whenever it reaches its destination, to prevent it from being automatically decrypted when it arrives at the server, just select the option that begins with Skip.. Below is some information about using each of them.

| TypeShipping | Encryption/Decryption Speed | Security | Shipping Size | Recommended Use |
|:---:|:---|:---|:---:|:---|
| None  | 37000pps Extremely Fast  | Not Safe | Very Little | Argument/Command Line |
| AES  | 31000pps Fast | Moderate  | Little | Coordinates/Actions of a player (game) | 
| RSA  | 1000ps Extremely Slow  | Extremely safe | Big | Login/Messages |
| Base64  | 36000pps Very Fast  | Not Safe | Little | Infos/Status simples |
| Compress  | 30000pps Fast  | Not Safe | Extremely Little | Video/Voice Call |
| OnlyBase64  | 36500pps Very Fast  | Not Safe | Little | Infos/Status simples |
| OnlyCompress  | 33000pps Fast  | Not Safe | Extremely Little | Video/Voice Call |

_Encryption/Decryprion Speed may vary depending on your machine's performance._

-----

<a name="TypeHoldConnection"></a>
### TypeHoldConnection

• TypeHoldConnection.None - Value default. (No effect)
• TypeHoldConnection.NotEnqueue - With NotEnqueue, shipments are sent to their destination without packet loss, shipments will not be sent in a queue to improve performance. (PS: it is possible that the same byte is received twice, increase the value of ReceiveAndSendTimeOut if this happens)
• TypeHoldConnection.Enqueue - With Enqueue, bytes are sent to their destination without packet loss, shipments will be sent in a queue, this feature is not recommended to be used for high demand for shipments, each package can vary between 1ms and 1000ms. (PS: it is possible that the same byte is received twice, increase the value of ReceiveAndSendTimeOut if this happens)

-----

<a name="ClientStatusConnection"></a>
### ClientStatusConnection
```cs
public enum ClientStatusConnection{
    Disconnected = 0, // Disconnected by the server
    Disconnecting = 1,
    Connected = 2,
    Connecting = 3,
    ConnectionFail = 4, // No connection to the server
    IpBlocked = 5, // IP blocked by the server
    MaxClientExceeded = 6, // The server has exceeded the limit of connected clients
}
```
The ClientStatusConnection is used to define client states. The ClientStatusConnection can be obtained by the [UDP.Client.Status](#UDP.ClientStatus) variable or with the event [UDP.Client.ClientStatusConnection](#OnClientStatus)

## FAQ

<a name="GroupID"></a>
### GroupID
GroupID is a way to organize your shipments with high performance, whenever you send bytes with the UDP.Server or Client the GroupID will be obtained in the following events: [UDP.Server.OnReceivedBytes](#OnReceivedBytesClient) and [UDP.Client.OnReceivedBytes](#OnReceivedBytesServer).

-----

<a name="ShippingPreparation"></a>
### Shipping Preparation

![Preview](/Images/ShippingPreparation.png)

After the message "Hello World!" was sent with [UDP.Client.SendBytes](#UDP.ClientSendBytes), the bytes were sorted before being sent, for Nethostfire features to work.