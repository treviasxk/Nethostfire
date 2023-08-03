
# Documentation Server and Client - UDP
- UDpServer
    - [UDpServer.Start](#UDpServerStart)
    - [UDpServer.Restart](#UDpServerRestart)
    - [UDpServer.Stop](#UDpServerStop)
    - [UDpServer.SendBytes](#UDpServerSendBytes)
    - [UDpServer.SendBytesGroup](#SendBytesGroup)
    - [UDpServer.SendBytesAll](#SendBytesAll)
    - [UDpServer.DisconnectClient](#DisconnectClient)
    - [UDpServer.DisconnectClientGroup](#DisconnectClientGroup)
    - [UDpServer.DisconnectClientAll](#DisconnectClientAll)
    - [UDpServer.ChangeLimitMaxByteSizeGroupID](#ChangeLimitMaxByteSizeGroupID)
    - [UDpServer.ChangeLimitMaxPacketsPerSecondsGroupID](#ChangeLimitMaxPacketsPerSecondsGroupID)
    - [UDpServer.ChangeBlockIP](#ChangeBlockIP)
    - [UDpServer.Socket](#Socket)
    - [UDpServer.LimitMaxByteReceive](#LimitMaxByteReceive)
    - [UDpServer.LimitMaxPacketsPerSeconds](#LimitMaxPacketsPerSeconds)
    - [UDpServer.Status](#Status)
    - [UDpServer.LostPackets](#LostPackets)
    - [UDpServer.MaxClients](#MaxClients)
    - [UDpServer.PacketsPerSeconds](#PacketsPerSeconds)
    - [UDpServer.PacketsBytesReceived](#PacketsBytesReceived)
    - [UDpServer.PacketsBytesSent](#PacketsBytesSent)
    - [UDpServer.ReceiveAndSendTimeOut](#ReceiveAndSendTimeOut)
    - [UDpServer.ShowUnityNetworkStatistics](#UdpServerShowUnityNetworkStatistics)
    - [UDpServer.ClientsCount](#ClientsCount)
    - [UDpServer.ShowDebugConsole](#ShowDebugConsole)
    - [UDpServer.OnConnectedClient](#OnConnectedClient)
    - [UDpServer.OnDisconnectedClient](#OnDisconnectedClient)
    - [UDpServer.ServerStatusConnection](#OnServerStatusConnection)
    - [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient);
- UDpClient
    - [UDpClient.Connect](#UDpClientConnect)
    - [UDpClient.SendBytes](#UDpClientSendBytes)
    - [UDpClient.DisconnectServer](#DisconnectServer)
    - [UDpClient.Socket](#Socket)
    - [UDpClient.Status](#UDpClientStatus)
    - [UDpClient.LostPackets](#UDpClientLostPackets)
    - [UDpClient.PacketsPerSeconds](#UDpClientPacketsPerSeconds)
    - [UDpClient.PacketsBytesReceived](#UDpClientPacketsBytesReceived)
    - [UDpClient.PacketsBytesSent](#UDpClientPacketsBytesSent)
    - [UDpClient.ReceiveAndSendTimeOut](#UDpClientReceiveAndSendTimeOut)
    - [UDpClient.ConnectTimeOut](#ConnectTimeOut)
    - [UDpClient.ShowUnityNetworkStatistics](#ShowUnityNetworkStatistics)
    - [UDpClient.ShowDebugConsole](#ShowDebugConsole)
    - [UDpClient.Ping](#Ping)
    - [UDpClient.OnReceivedBytesServer](#OnReceivedBytesServer)
    - [UDpClient.ClientStatusConnection](#OnClientStatusConnection)
    - [UDpClient.PublicKeyRSA](#PublicKeyRSA)
    - [UDpClient.PrivateKeyAES](#PrivateKeyAES)
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

## UDpServer

<a name="UDpServerStart"></a>
### UDpServer.Start
`UDpServer.Start(IPAddress _ip, int _port, int _symmetricSizeRSA = 86)`
```cs
UDpServer.Start(IPAddress.Any, 25000, 16);
```
Start the server with specific IP, Port and sets the size of [SymmetricSizeRSA](#SymmetricSizeRSA) if needed. If the server has already been started and then stopped you can call ``UDpServer.Start();`` without defining _host and _symmetricSizeRSA to start the server with the previous settings.

-----

<a name="UDpServerRestart"></a>
### UDpServer.Restart
`UDpServer.Restart()`
```cs
UDpServer.Restart();
```
If the server is running, you can restart it, all connected clients will be disconnected from the server and new RSA and AES keys will be generated again.

-----

<a name="UDpServerStop"></a>
### UDpServer.Stop
`UDpServer.Stop()`
```cs
UDpServer.Stop();
```
If the server is running, you can stop it, all connected clients will be disconnected from the server and if you start the server again new RSA and AES keys will be generated.

-----

<a name="UDpServerSendBytes"></a>
### UDpServer.SendBytes
`UDpServer.SendBytes(byte[] _byte, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, bool _holdConnection = false)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
   // Sending only groupID to client, without encryption and without HoldConnection
   UDpServer.SendBytes(null, 4, _dataClient);

   var _bytes = System.Text.Encoding.ASCII.GetBytes("Hello world!");

   // Sending bytes with groupID to client, without encryption and without HoldConnection
   UDpServer.SendBytes(_byte, 4, _dataClient);

   // Sending bytes with groupID to client, with encryption AES and without HoldConnection
   UDpServer.SendBytes(_byte, 4, _dataClient, TypeShipping.AES);

   // Sending bytes with groupID to client, with encryption RSA and with HoldConnection
   UDpServer.SendBytes(_byte, 4, _dataClient, TypeShipping.RSA, true);
}
```
To send bytes to a client, it is necessary to define the bytes, [GroupID](#GroupID) and [DataClient](#DataClient), the other sending resources such as [TypeShipping](#TypeShipping) and [HoldConnection](#HoldConnection) are optional.

-----

<a name="SendBytesGroup"></a>
### UDpServer.SendBytesGroup
`UDpServer.SendBytesGroup(byte[] _byte, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeShipping _typeShipping = TypeShipping.None, DataClient _skipDataClient = null, bool _holdConnection = false)`
```cs
using System.Collections.Concurrent;
static ConcurrentQueue<DataClient> PlayersLobby = new ConcurrentQueue<DataClient>();
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
   // Sending only groupID to all clients on the list, without encryption and without HoldConnection
   UDpServer.SendBytesGroup(null, 4, _dataClient);

   var _bytes = System.Text.Encoding.ASCII.GetBytes("Play Game");

   // Sending bytes with groupID to all clients on the list
   UDpServer.SendBytesGroup(_byte, 4, PlayersLobby);

   // Sending bytes with groupID to all clients on the list, except for the sending client.
   UDpServer.SendBytesGroup(_byte, 4, PlayersLobby, _skipDataClient: _dataClient);
}
```
To send bytes to a group client, it is necessary to define the bytes, [GroupID](#GroupID) and [List DataClient](#DataClient), the other sending resources such as [TypeShipping](#TypeShipping), SkipDataClient and [HoldConnection](#HoldConnection) are optional.

-----

<a name="SendBytesAll"></a>
### UDpServer.SendBytesAll
`UDpServer.SendBytesAll(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, DataClient _skipDataClient = null, bool _holdConnection = false)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
   // Sending only groupID to all clients connected, without encryption and without HoldConnection
   UDpServer.SendBytesAll(null, 4, _dataClient);

   var _bytes = System.Text.Encoding.ASCII.GetBytes("Play Game");

   // Sending bytes with groupID to all clients connected.
   UDpServer.SendBytesAll(_byte, 4);

   // Sending bytes with groupID to all clients connected, except for the sending client.
   UDpServer.SendBytesAll(_byte, 4, PlayersLobby, _skipDataClient: _dataClient);
}
```
To send bytes to all clients, it is necessary to define the bytes, [GroupID](#GroupID), the other sending resources such as [TypeShipping](#TypeShipping), SkipDataClient and [HoldConnection](#HoldConnection) are optional.

-----

<a name="DisconnectClient"></a>
### UDpServer.DisconnectClient
`UDpServer.DisconnectClient(DataClient _dataClient)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  UDpServer.DisconnectClient(_dataClient);
}
```
To disconnect a client from server, it is necessary to inform the [DataClient](#DataClient).

-----

<a name="DisconnectClientGroup"></a>
### UDpServer.DisconnectClientGroup
`UDpServer.DisconnectClientGroup(<List>DataClient _dataClient)`
```cs
using System.Collections.Concurrent;
static ConcurrentQueue<DataClient> AFKPlayers = new ConcurrentQueue<DataClient>();
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  UDpServer.DisconnectClientGroup(AFKPlayers);
}
```
To disconnect a group clients from server, it is necessary to inform the [List DataClient](#DataClient).

-----

<a name="DisconnectClientAll"></a>
### UDpServer.DisconnectClientAll
`UDpServer.DisconnectClientAll()`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  UDpServer.DisconnectClientAll();
}
```
To disconnect alls clients from server.

-----

<a name="ChangeLimitMaxByteSizeGroupID"></a>
### UDpServer.ChangeLimitMaxByteSizeGroupID
`UDpServer.ChangeLimitMaxByteSizeGroupID(int _groupID, int _limitBytes)`
```cs
UDpServer.ChangeLimitMaxByteSizeGroupID(4, 12);
```
The ChangeLimitMaxByteSizeGroupID will change the maximum limit of bytes of a [GroupID](#GroupID) that the server will read when receiving the bytes, if the packet bytes is greater than the limit, the server will not call the [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient) event with the received bytes. The default value _limitBytes is 0 which is unlimited.

-----

<a name="ChangeLimitMaxPacketsPerSecondsGroupID"></a>
### UDpServer.ChangeLimitMaxPacketsPerSecondsGroupID
`UDpServer.ChangeLimitMaxPacketsPerSecondsGroupID(int _groupID, int _limitPPS)`
```cs
UDpServer.ChangeLimitMaxPacketsPerSecondsGroupID(4, 60);
```
The ChangeLimitMaxPacketsPerSecondsGroupID will change the maximum limit of Packets per seconds (PPS) of a [GroupID](#GroupID), if the packets is greater than the limit in 1 second, the server will not call the [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient) event with the received bytes. The default value is _limitBytes 0 which is unlimited.

-----

<a name="ChangeBlockIP"></a>
### UDpServer.ChangeBlockIP
`UDpServer.ChangeBlockIP(IPEndPoint _ip, int _time)`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  // IP blocked per 1 hour
  UDpServer.ChangeBlockIP(_dataClient.IP, 60000 * 60);
}
```
ChangeBlockIP blocks a specific IP for the time defined in milliseconds. If the time is 0 the IP will be removed from the server's blocked IP list.

-----

<a name="ClientsCount"></a>
### UDpServer.ClientsCount
`Read-Only Variable`
```cs
static void OnConnectedClient(DataClient _dataClient){
    int onlines = UDpServer.ClientsCount;
    Console.WriteLine("Has a total of {0} players connected.", onlines);
}
```
The ClientsCount is the total number of clients connected to the server.

-----

<a name="LimitMaxByteReceive"></a>
### UDpServer.LimitMaxByteReceive
`Write/Read Variable`
```cs
// Limit in 12 bytes;
UDpServer.LimitMaxByteReceive = 12;
```
The LimitMaxByteReceive will change the maximum limit of bytes that the server will read when receiving, if the packet bytes is greater than the limit, the server will not call the [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient) event with the received bytes. The default value is 0 which is unlimited.

-----

<a name="LimitMaxPacketsPerSeconds"></a>
### UDpServer.LimitMaxPacketsPerSeconds
`Write/Read Variable`
```cs
// Limit in 60 pps;
UDpServer.LimitMaxPacketsPerSeconds = 60;
```
The LimitMaxPacketsPerSeconds will change the maximum limit of Packets per seconds (PPS), if the packets is greater than the limit in 1 second, the server will not call the [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient) event with the received bytes. The default value is 0 which is unlimited.

-----

<a name="LostPackets"></a>
### UDpServer.LostPackets
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  int LostPackets = UDpServer.LostPackets;
  Console.WriteLine("{0} packets lost", LostPackets);
}
```
LostPackets is the number of packets lost.

-----

<a name="MaxClients"></a>
### UDpServer.MaxClients
`Write/Read Variable`
```cs
UDpServer.MaxClients = 32; // Maximum 32 Clients
```
MaxClients is the maximum number of clients that can connect to the server. If you have many connected clients and you change the value below the number of connected clients, they will not be disconnected, the server will block new connections until the number of connected clients is below or equal to the limit.

-----

<a name="PacketsPerSeconds"></a>
### UDpServer.PacketsPerSeconds
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsPerSeconds = UDpServer.PacketsPerSeconds;
  Console.WriteLine("{0} Packets Per Seconds", packetsPerSeconds);
}
```
PacketsPerSeconds is the amount of packets per second that happen when the server is sending and receiving.

-----

<a name="PacketsBytesReceived"></a>
### UDpServer.PacketsBytesReceived
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesReceived = UDpServer.PacketsBytesReceived;
  Console.WriteLine("Received: {0}", packetsBytesReceived);
}
```
PacketsBytesReceived is the amount of bytes received by the server.

-----

<a name="PacketsBytesSent"></a>
### UDpServer.PacketsBytesSent
`Read-Only Variable`
```cs
static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesSent = UDpServer.PacketsBytesSent;
  Console.WriteLine("Sent: {0}", packetsBytesSent);
}
```
PacketsBytesSent is the amount of bytes sent by the server.

-----

<a name="ReceiveAndSendTimeOut"></a>
### UDpServer.ReceiveAndSendTimeOut
`Write/Read Variable`
```cs
UDpServer.ReceiveAndSendTimeOut = 2000;
```
ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the server will ignore the receiving or sending. The default and recommended value is 1000.

-----

<a name="ShowDebugConsole"></a>
### UDpServer.ShowDebugConsole or UDpClient.ShowDebugConsole
`Write/Read Variable`
```cs
UDpServer.ShowDebugConsole = false;
// Or
UDpClient.ShowDebugConsole = false;
```
![Preview](/Images/DebugConsole.png)

The ShowDebugConsole when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. The default value is true.

-----

<a name="Socket"></a>
### UDpServer.Socket or UDpClient.Socket
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
### UDpServer.Status
`Write/Read Variable`
```cs
if(UDpServer.Status == ServerStatusConnection.Running){
    // UDpServer Running.
}
```
The Status is an enum [UDpServer.ServerStatusConnection](#ServerStatusConnection) with it you can know the current state of the server.

-----

<a name="OnConnectedClient"></a>
### UDpServer.OnConnectedClient
`Event`
```cs
static void Main(string[] args){
    UDpServer.OnConnectedClient += OnConnectedClient;
    UDpServer.Start(IPAddress.Any, 25000);
}

static void OnConnectedClient(DataClient _dataClient){
    Console.WriteLine(_dataClient.IP + " new client conected!");
}
```
OnConnectedClient is an event that you can use to receive the [DataClient](#DataClient) whenever a new client connected.

-----

<a name="OnDisconnectedClient"></a>
### UDpServer.OnDisconnectedClient
`Event`
```cs
static void Main(string[] args){
    UDpServer.OnDisconnectedClient += OnDisconnectedClient;
    UDpServer.Start(IPAddress.Any, 25000);
}

static void OnDisconnectedClient(DataClient _dataClient){
    Console.WriteLine(_dataClient.IP + " new client conected!");
}
```
OnDisconnectedClient is an event that you can use to receive the [DataClient](#DataClient) whenever a new client disconnected.

-----

<a name="OnServerStatusConnection"></a>
### UDpServer.ServerStatusConnection
`Event`
```cs
static void Main(string[] args){
    UDpServer.ServerStatusConnection += OnServerStatusConnection;
    UDpServer.Start(IPAddress.Any, 25000);
}

static void OnServerStatusConnection(ServerStatusConnection _status){
    Console.WriteLine("UDpServer Status: " + _status);
}
```
OnServerStatusConnection is an event that returns [UDpServer.ServerStatusConnection](#ServerStatusConnection) whenever the status changes, with which you can use it to know the current status of the server.

-----

<a name="OnReceivedBytesClient"></a>
### UDpServer.OnReceivedBytesClient
`Event`
```cs
static void Main(string[] args){
    UDpServer.OnReceivedBytesClient += OnReceivedBytesClient;
    UDpServer.Start(IPAddress.Any, 25000);
}

static void OnReceivedBytesClient(byte[] _byte, int _groupID, DataClient _dataClient){
    Console.WriteLine("[RECEIVED] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
}
```
OnReceivedBytesClient an event that returns bytes received, [GroupID](#GroupID) and [DataClient](#DataClient) whenever the received bytes by clients, with it you can manipulate the bytes received.

## Client
<a name="UDpClientConnect"></a>
### UDpClient.Connect
`Connect(IPEndPoint _host, int _symmetricSizeRSA = 86)`
```cs
UDpClient.Connect(IPAddress.Parse("127.0.0.1"), 25000, 20);
```
Connect to a server with IP, Port and sets the size of [SymmetricSizeRSA](#SymmetricSizeRSA) if needed.

-----

<a name="UDpClientSendBytes"></a>
### UDpClient.SendBytes
`SendBytes(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, bool _holdConnection = false)`
```cs
var _bytes = System.Text.Encoding.ASCII.GetBytes("Hello world!");

// Sending only groupID, without encryption and without HoldConnection
UDpClient.SendBytes(null, 11);

// Sending bytes with groupID 4 to server, without encryption and without HoldConnection
UDpClient.SendBytes(_byte, 4);

// Sending bytes with groupID 4 to server, with encryption AES and without HoldConnection
UDpClient.SendBytes(_byte, 4, TypeShipping.AES);

// Sending bytes with groupID 4 to server, with encryption RSA and with HoldConnection
UDpClient.SendBytes(_byte, 4, TypeShipping.RSA, true);
```
To send bytes to server, it is necessary to define the bytes and [GroupID](#GroupID), the other sending resources such as [TypeShipping](#TypeShipping) and [HoldConnection](#HoldConnection) are optional.

-----

<a name="DisconnectServer"></a>
### UDpClient.DisconnectServer
`UDpClient.DisconnectServer()`
```cs
UDpClient.DisconnectServer();
```
With DisconnectServer the client will be disconnected from the server.

-----

<a name="UDpClientLostPackets"></a>
### UDpClient.LostPackets
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  int LostPackets = UDpClient.LostPackets;
  Console.WriteLine("{0} packets lost", LostPackets);
}
```
LostPackets is the number of packets lost.

-----

<a name="UDpClientPacketsPerSeconds"></a>
### UDpClient.PacketsPerSeconds
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsPerSeconds = UDpClient.PacketsPerSeconds;
  Console.WriteLine("{0} Packets Per Seconds", packetsPerSeconds);
}
```
PacketsPerSeconds is the amount of packets per second that happen when the client is sending and receiving.

-----

<a name="UDpClientPacketsBytesReceived"></a>
### UDpClient.PacketsBytesReceived
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesReceived = UDpClient.PacketsBytesReceived;
  Console.WriteLine("Received: {0}", packetsBytesReceived);
}
```
PacketsBytesReceived is the amount of bytes received by the client.

-----

<a name="UDpClientPacketsBytesSent"></a>
### UDpClient.PacketsBytesSent
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string packetsBytesSent = UDpClient.PacketsBytesSent;
  Console.WriteLine("Sent: {0}", packetsBytesSent);
}
```
PacketsBytesSent is the amount of bytes sent by the client.

-----

<a name="UDpClientReceiveAndSendTimeOut"></a>
### UDpClient.ReceiveAndSendTimeOut
`Write/Read Variable`
```cs
UDpClient.ReceiveAndSendTimeOut = 2000;
```
ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the client will ignore the receiving or sending. The default and recommended value is 1000.

-----

<a name="OnReceivedBytesServer"></a>
### UDpClient.OnReceivedBytesServer
`Event`
```cs
static void Main(string[] args){
    UDpClient.OnReceivedBytesServer += OnReceivedBytesServer;
    UDpClient.Connect(IPAddress.Parse("127.0.0.1"), 25000);
}

static void OnReceivedBytesServer(byte[] _byte, int _groupID){
    Console.WriteLine("[RECEIVED] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
}
```
OnReceivedBytesServer an event that returns bytes received and [GroupID](#GroupID) whenever the received bytes by clients, with it you can manipulate the bytes received.

-----

<a name="OnClientStatusConnection"></a>
### UDpClient.ClientStatusConnection
`Event`
```cs
static void Main(string[] args){
    UDpClient.ClientStatusConnection += OnClientStatusConnection;
    UDpServer.Start(IPAddress.Any, 25000);
}

static void OnClientStatusConnection(ClientStatusConnection _status){
    Console.WriteLine("Client Status: " + _status);
}
```
OnClientStatusConnection is an event that returns [UDpClient.ClientStatusConnection](#ClientStatusConnection) whenever the status changes, with which you can use it to know the current status of the server.

-----

<a name="Ping"></a>
### UDpClient.Ping
`Read-Only Variable`
```cs
static void OnReceivedBytesServer(byte[] _byte, int _groupID, DataClient _dataClient){
  string ping = UDpClient.Ping;
  Console.WriteLine("PING: {0}ms", ping);
}
```
Ping returns an integer value, this value is per milliseconds

-----

<a name="PublicKeyRSA"></a>
### UDpClient.PublicKeyRSA
`Read-Only Variable`
```cs
static void OnClientStatusConnection(ClientStatusConnection _status){
    if(_status == ClientStatusConnection.Connected){
        string publicKeyRSA = UDpClient.PublicKeyRSA;
        Console.WriteLine("Public Key RSA: {0}", publicKeyRSA);
    }
}
```
PublicKeyRSA returns the RSA public key obtained by the server after connecting.

-----

<a name="PrivateKeyAES"></a>
### UDpClient.PrivateKeyAES
`Read-Only Variable`
```cs
static void OnClientStatusConnection(ClientStatusConnection _status){
    if(_status == ClientStatusConnection.Connected){
        string privateKeyAES = UDpClient.PrivateKeyAES;
        Console.WriteLine("Private Key AES: {0}", privateKeyAES);
    }
}
```
PrivateKeyAES returns the AES private key obtained by the server after connecting.

-----

<a name="UDpClientStatus"></a>
### UDpClient.Status
`Write/Read Variable`
```cs
if(UDpClient.Status == ClientStatusConnection.Connected){
    // Client Connected.
}
```
The Status is an enum [UDpClient.ClientStatusConnection](#ClientStatusConnection) with it you can know the current state of the client.

-----

<a name="ConnectTimeOut"></a>
### UDpClient.ConnectTimeOut
`Write/Read Variable`
```cs
UDpClient.ConnectTimeOut = 15000; // 15s
```
ConnectTimeOut is the time the client will be reconnecting with the server, the time is defined in milliseconds, if the value is 0 the client will be reconnecting infinitely. The default value is 10000.

-----

<a name="ShowUnityNetworkStatistics"></a>
### UDpClient.ShowUnityNetworkStatistics
`Write/Read Variable`
```cs
UDpClient.ShowUnityNetworkStatistics = true;
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
The DataClient class is used to store data from a client on the server. It is with this class that the server uses to define a client. The DataClients can be obtained with the following server events [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient), [UDpClient.OnReceivedBytesServer](#OnReceivedBytesServer), [UDpServer.OnConnectedClient](#OnConnectedClient) and [UDpServer.OnDisconnectedClient](#OnDisconnectedClient)

-----

<a name="ServerStatusConnection"></a>
### UDpServer.ServerStatusConnection
```cs
public enum ServerStatusConnection{
    Stopped = 0,
    Stopping = 1,
    Running = 2,
    Initializing = 3,
    Restarting = 4,
}
```
The ServerStatusConnection is used to define server states. The ServerStatusConnection can be obtained by the [UDpServer.Status](#Status) variable or with the event [UDpServer.ServerStatusConnection](#OnServerStatusConnection)

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
• TypeHoldConnection.Auto - With Auto, when the packet arrives at its destination, the Client/Server will automatically respond back confirming receipt.
• TypeHoldConnection.Manual - With Manual, when the packet arrives at its destination, it is necessary that the Client/Server responds back by sending any byte for the same GroupID received. If it doesn't respond, the client/server that sent the Manual will be stuck in a send loop.
• TypeHoldConnection.Enqueue - With Enqueue the bytes are adds in a queue and sent 1 packet at a time, sending is done with HoldConnection in Auto. This feature is not recommended to be used for high demand for shipments, each package can vary between 1ms and 1000ms.

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
The ClientStatusConnection is used to define client states. The ClientStatusConnection can be obtained by the [UDpClient.Status](#UDpClientStatus) variable or with the event [UDpClient.ClientStatusConnection](#OnClientStatusConnection)

## FAQ

<a name="GroupID"></a>
### GroupID
GroupID is a way to organize your shipments with high performance, whenever you send bytes with the UDpServer or Client the GroupID will be obtained in the following events: [UDpServer.OnReceivedBytesClient](#OnReceivedBytesClient) and [UDpClient.OnReceivedBytesServer](#OnReceivedBytesServer).

-----

<a name="ShippingPreparation"></a>
### Shipping Preparation

![Preview](/Images/ShippingPreparation.png)

After the message "Hello World!" was sent with [UDpClient.SendBytes](#UDpClientSendBytes), the bytes were sorted before being sent, for Nethostfire features to work.