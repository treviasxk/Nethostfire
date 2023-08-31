// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace Nethostfire {
   public class UDpServer {
      static IPEndPoint host;
      public static UdpClient Socket;
      static bool showUnityNetworkStatistics = false;
      static int packetsCount, packetsTmp, timeTmp, symmetricSizeRSA, limitMaxPPS = 0, maxClients = 32, lostPackets, limitMaxByteSize = 0;
      static float packetsReceived, packetsReceived2, packetsSent, packetsSent2;
      static ConcurrentDictionary<int, int> ListLimitMaxByteSizeGroupID = new();
      static ConcurrentDictionary<int, LimitMaxPPS> ListLimitMaxPPSGroupID = new();
      static ConcurrentDictionary<IPEndPoint, DataClient> DataClients = new();
      static ConcurrentDictionary<IPEndPoint, DataClient> WaitDataClients = new();
      static ConcurrentDictionary<IPEndPoint, long> ListBlockedIPs = new();
      static Thread CheckOnlineThread, ServerReceiveUDPThread;
      /// <summary>
      /// OnReceivedBytes an event that returns bytes received, GroupID and DataClient whenever the received bytes by clients, with it you can manipulate the bytes received.
      /// </summary>
      public static Action<byte[], int, DataClient> OnReceivedBytes;
      /// <summary>
      /// OnServerStatus is an event that returns Server.ServerStatusConnection whenever the status changes, with which you can use it to know the current status of the server.
      /// </summary>
      public static Action<ServerStatusConnection> OnServerStatus;
      /// <summary>
      /// OnConnectedClient is an event that you can use to receive the DataClient whenever a client connected.
      /// </summary>
      public static Action<DataClient> OnConnectedClient;
      /// <summary>
      /// OnDisconnectedClient is an event that you can use to receive the DataClient whenever a client disconnected.
      /// </summary>
      public static Action<DataClient> OnDisconnectedClient;
      /// <summary>
      /// The Status is an enum Server.ServerStatusConnection with it you can know the current state of the server.
      /// </summary>
      public static ServerStatusConnection Status {get;set;} = ServerStatusConnection.Stopped;
      /// <summary>
      /// ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the server will ignore the receiving or sending. The default and recommended value is 1000.
      /// </summary>
      public static int ReceiveAndSendTimeOut {get {return Utility.receiveAndSendTimeOut;} set{if(Socket != null){Socket.Client.ReceiveTimeout = value; Socket.Client.SendTimeout = value;} Utility.receiveAndSendTimeOut = value > 0 ? value : 1000;}}
      /// <summary>
      /// The LimitMaxPacketsPerSeconds will change the maximum limit of Packets per seconds (PPS), if the packets is greater than the limit in 1 second, the server will not call the Server.OnReceivedBytes event with the received bytes. The default value is 0 which is unlimited.
      /// </summary>
      public static int LimitMaxPacketsPerSeconds {get {return limitMaxPPS;} set{limitMaxPPS = value;}}
      /// <summary>
      /// The LimitMaxByteReceive will change the maximum limit of bytes that the server will read when receiving, if the packet bytes is greater than the limit, the server will not call the Server.OnReceivedBytes event with the received bytes. The default value is 0 which is unlimited.
      /// </summary>
      public static int LimitMaxByteReceive {get {return limitMaxByteSize;} set{limitMaxByteSize = value;}}
      /// <summary>
      /// The ClientsCount is the total number of clients connected to the server.
      /// </summary>
      public static int ClientsCount {get {return DataClients.Count;}}
      /// <summary>
      /// MaxClients is the maximum number of clients that can connect to the server. If you have many connected clients and you change the value below the number of connected clients, they will not be disconnected, the server will block new connections until the number of connected clients is below or equal to the limit.
      /// </summary>
      public static int MaxClients {get {return maxClients;} set{maxClients = value;}}
      /// <summary>
      /// LostPackets is the number of packets lost.
      /// </summary>
      public static int LostPackets {get {return lostPackets;}}
      /// <summary>
      /// PacketsPerSeconds is the amount of packets per second that happen when the server is sending and receiving.
      /// </summary>
      public static string PacketsPerSeconds {get {return packetsCount + "pps";}}
      /// <summary>
      /// PacketsBytesReceived is the amount of bytes received by the server.
      /// </summary>
      public static string PacketsBytesReceived {get {return Utility.BytesToString(packetsReceived2);}}
      /// <summary>
      /// When using Nethostfire in Unity and when set the value of ShowUnityNetworkStatistics to true, statistics on server will be displayed in console running batchmode.
      /// </summary>
      public static bool ShowUnityNetworkStatistics {get {return showUnityNetworkStatistics;} set {showUnityNetworkStatistics = value;}}
      /// <summary>
      /// The ShowDebugConsole when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. The default value is true.
      /// </summary>
      public static bool ShowDebugConsole {get {return Utility.ShowDebugConsole;} set { Utility.ShowDebugConsole = value;}}
      /// <summary>
      /// PacketsBytesSent is the amount of bytes sent by the server.
      /// </summary>
      public static string PacketsBytesSent {get {return Utility.BytesToString(packetsSent2);}}
      /// <summary>
      /// Start the server with specific IP, Port and sets the size of SymmetricSizeRSA if needed. If the server has already been started and then stopped you can call Server.Start(); without defining _host and _symmetricSizeRSA to start the server with the previous settings.
      /// </summary>
      public static void Start(IPAddress _ip, int _port, int _symmetricSizeRSA = 86){
         if(Status == ServerStatusConnection.Stopped || Status == ServerStatusConnection.Restarting)
            ChangeStatus(ServerStatusConnection.Initializing);
         if(Socket is null && _ip is not null){
            Socket = new UdpClient();
            Socket.Client.SendTimeout = Utility.receiveAndSendTimeOut;
            Socket.Client.ReceiveTimeout = Utility.receiveAndSendTimeOut;
            symmetricSizeRSA = _symmetricSizeRSA;
            Utility.GenerateKey(TypeUDP.Server, _symmetricSizeRSA);
            host = new IPEndPoint(_ip, _port);
            try{
               Socket.Client.Bind(host);
            }catch{
               Socket = null;
               ChangeStatus(ServerStatusConnection.Stopped);
               throw new Exception(Utility.ShowLog("Could not start the server, check that the port "+ _port + " is not blocked, or that you have other software using that port."));
            }
            if(ServerReceiveUDPThread == null){
               ServerReceiveUDPThread = new Thread(ServerReceiveUDP)
               {
                  IsBackground = true,
                  Priority = ThreadPriority.Highest
               };
               ServerReceiveUDPThread.SetApartmentState(ApartmentState.MTA);
               ServerReceiveUDPThread.Start();
            }
            if(CheckOnlineThread == null){
               CheckOnlineThread = new Thread(CheckOnline)
               {
                  IsBackground = true,
                  Priority = ThreadPriority.Highest
               };
               CheckOnlineThread.SetApartmentState(ApartmentState.MTA);
               CheckOnlineThread.Start();
            }
         }else
            if(_ip is null)
               throw new Exception(Utility.ShowLog("It is not possible to start the server, without the _ip having been configured beforehand."));
         if(Status == ServerStatusConnection.Initializing)
            ChangeStatus(ServerStatusConnection.Running);
         else
            Utility.StartUnity();
      }
      /// <summary>
      /// If the server is running, you can stop it, all connected clients will be disconnected from the server and if you start the server again new RSA and AES keys will be generated.
      /// </summary>
      public static void Stop(){
         if(Status == ServerStatusConnection.Running || Status == ServerStatusConnection.Restarting){
            if(Status == ServerStatusConnection.Running)
               ChangeStatus(ServerStatusConnection.Stopping);
            Socket.Close();
            Socket = null;
            ServerReceiveUDPThread = null;
            CheckOnlineThread = null;
            DataClients.Clear();
            WaitDataClients.Clear();
            if(Status == ServerStatusConnection.Stopping)
               ChangeStatus(ServerStatusConnection.Stopped);
         }
      }
      /// <summary>
      /// If the server is running, you can restart it, all connected clients will be disconnected from the server and new RSA and AES keys will be generated again.
      /// </summary>
      public static void Restart(){
         if(Status == ServerStatusConnection.Running){
            ChangeStatus(ServerStatusConnection.Restarting);
            Stop();
            Start(host.Address, host.Port, symmetricSizeRSA);
         }else
            throw new Exception(Utility.ShowLog("It is not possible to restart the server if it is not running."));
      }



      /// <summary>
      /// To send bytes to a client, it is necessary to define the Bytes, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(byte[] _byte, int _groupID, DataClient _dataClient){
         PrepareSend(_byte, _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None);
      }
      /// <summary>
      /// To send bytes to a client, it is necessary to define the Bytes, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(byte[] _byte, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         PrepareSend(_byte, _groupID, _dataClient, _typeShipping, _typeHoldConnection);
      }
      /// <summary>
      /// To send bytes to a client, it is necessary to define the Bytes, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(byte[] _byte, int _groupID, DataClient _dataClient, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         PrepareSend(_byte, _groupID, _dataClient, _typeShipping, _typeHoldConnection);
      }



      /// <summary>
      /// To send string to a client, it is necessary to define the Text, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(string _text, int _groupID, DataClient _dataClient){
         PrepareSend(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None);
      }
      /// <summary>
      /// To send string to a client, it is necessary to define the Text, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(string _text, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         PrepareSend(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, _typeShipping, _typeHoldConnection);
      }
      /// <summary>
      /// To send string to a client, it is necessary to define the Text, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(string _text, int _groupID, DataClient _dataClient, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         PrepareSend(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, _typeShipping, _typeHoldConnection);
      }



      /// <summary>
      /// To send float to a client, it is necessary to define the Number, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(float _number, int _groupID, DataClient _dataClient){
         PrepareSend(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None);
      }
      /// <summary>
      /// To send float to a client, it is necessary to define the Number, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(float _number, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         PrepareSend(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, _typeShipping, _typeHoldConnection);
      }
      /// <summary>
      /// To send float to a client, it is necessary to define the Number, GroupID and DataClient, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
      /// </summary>
      public static void Send(float _number, int _groupID, DataClient _dataClient, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         PrepareSend(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, _typeShipping, _typeHoldConnection);
      }




      static void PrepareSend(byte[] _byte, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         if(Status == ServerStatusConnection.Running){
            if(!Utility.Send(Socket, _byte, _groupID, _typeShipping, _typeHoldConnection, _dataClient.PrivateKeyAES != null ? TypeContent.Foreground : TypeContent.Background, Utility.IndexShipping++, _dataClient))
               lostPackets++;
            else
               packetsSent += _byte.Length;
            packetsTmp++;
         }
      }



      /// <summary>
      /// To send bytes to a group client, it is necessary to define the Bytes, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(byte[] _byte, int _groupID, ConcurrentQueue<DataClient> _dataClients){
         Parallel.ForEach(_dataClients, _dataClient => Send(_byte, _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None));
      }
      /// <summary>
      /// To send bytes to a group client, it is necessary to define the Bytes, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(byte[] _byte, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         Parallel.ForEach(_dataClients, _dataClient => Send(_byte, _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }
      /// <summary>
      /// To send bytes to a group client, it is necessary to define the Bytes, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(byte[] _byte, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         Parallel.ForEach(_dataClients, _dataClient => Send(_byte, _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }




      /// <summary>
      /// To send string to a group client, it is necessary to define the Text, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(string _text, int _groupID, ConcurrentQueue<DataClient> _dataClients){
         Parallel.ForEach(_dataClients, _dataClient => Send(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None));
      }
      /// <summary>
      /// To send string to a group client, it is necessary to define the Text, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(string _text, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         Parallel.ForEach(_dataClients, _dataClient => Send(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }
      /// <summary>
      /// To send string to a group client, it is necessary to define the Text, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(string _text, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         Parallel.ForEach(_dataClients, _dataClient => Send(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }



      /// <summary>
      /// To send float to a group client, it is necessary to define the Number, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(float _number, int _groupID, ConcurrentQueue<DataClient> _dataClients){
         Parallel.ForEach(_dataClients, _dataClient => Send(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None));
      }
      /// <summary>
      /// To send float to a group client, it is necessary to define the Number, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(float _number, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         Parallel.ForEach(_dataClients, _dataClient => Send(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }
      /// <summary>
      /// To send float to a group client, it is necessary to define the Number, GroupID and ConcurrentQueue DataClient, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendGroup(float _number, int _groupID, ConcurrentQueue<DataClient> _dataClients, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         Parallel.ForEach(_dataClients, _dataClient => Send(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }



      /// <summary>
      /// To send bytes to all clients, it is necessary to define the Bytes, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(byte[] _byte, int _groupID){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(_byte, _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None));
      }
      /// <summary>
      /// To send bytes to all clients, it is necessary to define the Bytes, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(_byte, _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }
      /// <summary>
      /// To send bytes to all clients, it is necessary to define the Bytes, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(byte[] _byte, int _groupID, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(_byte, _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }


      /// <summary>
      /// To send string to all clients, it is necessary to define the Text, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(string _text, int _groupID){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None));
      }
      /// <summary>
      /// To send string to all clients, it is necessary to define the Text, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(string _text, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }
      /// <summary>
      /// To send string to all clients, it is necessary to define the Text, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(string _text, int _groupID, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(Encoding.UTF8.GetBytes(_text), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }


      /// <summary>
      /// To send float to all clients, it is necessary to define the Number, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(float _number, int _groupID){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, TypeShipping.None, TypeHoldConnection.None));
      }
      /// <summary>
      /// To send float to all clients, it is necessary to define the Number, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(float _number, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }
      /// <summary>
      /// To send float to all clients, it is necessary to define the Number, GroupID, the other sending resources such as TypeShipping, SkipDataClient and TypeHoldConnection are optional.
      /// </summary>
      public static void SendAll(float _number, int _groupID, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
         Parallel.ForEach(DataClients.Values, _dataClient => Send(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _dataClient, _typeShipping, _typeHoldConnection));
      }



      /// <summary>
      /// To disconnect a client from server, it is necessary to inform the DataClient.
      /// </summary>
      public static void DisconnectClient(DataClient _dataClient){
         if(Status == ServerStatusConnection.Running)
            Utility.SendPing(Socket, new byte[]{0},_dataClient);
      }
      /// <summary>
      /// To disconnect a group clients from server, it is necessary to inform the List DataClient.
      /// </summary>
      public static void DisconnectClientGroup(ConcurrentQueue<DataClient> _dataClients){
         Parallel.ForEach(_dataClients, dataClient => {
            if(Status == ServerStatusConnection.Running)
               Utility.SendPing(Socket, new byte[]{0}, dataClient);
         });
      }
      /// <summary>
      /// To disconnect alls clients from server.
      /// </summary>
      public static void DisconnectClientAll(){
         Parallel.ForEach(DataClients, item => {
            if(Status == ServerStatusConnection.Running)
               Utility.SendPing(Socket, new byte[]{0}, item.Value);
         });
         DataClients.Clear();
         WaitDataClients.Clear();
         Thread.Sleep(3000);
      }

      /// <summary>
      /// ChangeBlockIP blocks a specific IP for the time defined in milliseconds. If the time is 0 the IP will be removed from the server's blocked IP list.
      /// </summary>
      public static void ChangeBlockIP(IPEndPoint _ip, int _time){
         if(ListBlockedIPs.TryGetValue(_ip, out var _blockedIP)){    
            if(_time > 0)
               _blockedIP = _time;
            else    
               ListBlockedIPs.TryRemove(_ip, out _);
         }else{
            ListBlockedIPs.TryAdd(_ip, _time + Environment.TickCount);
            Utility.SendPing(Socket, new byte[]{2}, new DataClient(){IP = _ip});
         }
      }


      static bool CheckBlockerIP(IPEndPoint _ip){
         if(ListBlockedIPs.TryGetValue(_ip, out var _blockedIP)){
            if(_blockedIP < Environment.TickCount){
               ListBlockedIPs.TryRemove(_ip, out _);
               return false;
            }
            else{
               if(DataClients.TryRemove(_ip, out var _dataClient))
                  Utility.SendPing(Socket, new byte[]{2}, _dataClient);
               
               if(WaitDataClients.TryRemove(_ip, out var _waitDataClient))
                  Utility.SendPing(Socket, new byte[]{2}, _waitDataClient);
               
               return true;
            } 
         }else
            return false;
      }


      /// <summary>
      /// The ChangeLimitMaxByteSizeGroupID will change the maximum limit of bytes of a GroupID that the server will read when receiving the bytes, if the packet bytes is greater than the limit, the server will not call the Server.OnReceivedBytes event with the received bytes. The default value _limitBytes is 0 which is unlimited.
      /// </summary>
      public static void ChangeLimitMaxByteSizeGroupID(int _groupID, int _limitBytes){
         if(ListLimitMaxByteSizeGroupID.TryGetValue(_groupID, out var _limitMaxByteSizeGroupID)){
            if(_limitBytes < 0)
               _limitMaxByteSizeGroupID = _limitBytes;
            else
               ListLimitMaxByteSizeGroupID.TryRemove(_groupID, out _);
         }else
            ListLimitMaxByteSizeGroupID.TryAdd(_groupID, _limitBytes);
      }

      /// <summary>
      /// The ChangeLimitMaxPacketsPerSecondsGroupID will change the maximum limit of Packets per seconds (PPS) of a GroupID, if the packets is greater than the limit in 1 second, the server will not call the Server.OnReceivedBytes event with the received bytes. The default value is _limitBytes 0 which is unlimited.
      /// </summary>
      public static void ChangeLimitMaxPacketsPerSecondsGroupID(int _groupID, int _limitPPS){
         if(ListLimitMaxPPSGroupID.TryGetValue(_groupID, out var _limitMaxPPS)){
            if(_limitPPS < 0)
               _limitMaxPPS.PPS = _limitPPS;
            else
               ListLimitMaxPPSGroupID.TryRemove(_groupID, out _);
         }else{
                LimitMaxPPS limitMaxPPS = new LimitMaxPPS
                {
                    PPS = _limitPPS,
                    Timer = Environment.TickCount
                };
                ListLimitMaxPPSGroupID.TryAdd(_groupID, limitMaxPPS); 
         }
      }

      static async void ServerReceiveUDP(){
         while(Socket != null){
            byte[] data  =  null;
            IPEndPoint _ip = null;

            try{
               var receivedResult = await Socket.ReceiveAsync();
               data  = receivedResult.Buffer;
               _ip = receivedResult.RemoteEndPoint;
            }catch{}

            if(data != null && _ip != null)
            if(!CheckBlockerIP(_ip))
            Parallel.Invoke(()=>{
               packetsTmp++;
               if(DateTime.Now.Second != timeTmp){
                  timeTmp = DateTime.Now.Second;
                  packetsReceived2 = packetsReceived;
                  packetsSent2 = packetsSent;
                  packetsCount = packetsTmp;
                  packetsTmp = 0;
               }

               DataClient _dataClient = new();
               if(DataClients.TryGetValue(_ip, out var _client)){
                  _dataClient = _client;
                  _dataClient.TimeLastPacket = Environment.TickCount;
                  if(data.Length == 1){
                     switch(data[0]){
                        case 0:
                           Utility.RunOnMainThread(() => OnDisconnectedClient?.Invoke(_dataClient));
                           DataClients.TryRemove(_ip, out _);
                           Utility.ShowLog(_dataClient.IP + " disconnected from the server.");
                        break;
                        case 1:
                           _dataClient.Ping = Environment.TickCount - _dataClient.Time - 1000;
                           _dataClient.Time = Environment.TickCount;
                           Utility.SendPing(Socket, new byte[]{1}, _dataClient);
                        break;
                     }
                  }
               }else{
                  if(DataClients.Count >= maxClients){
                     Utility.SendPing(Socket, new byte[]{3}, new DataClient(){IP = _ip});
                     data = new byte[]{};
                  }
               }

               if(data.Length > 1 && Status == ServerStatusConnection.Running){
                  var _data = Utility.ByteToReceive(data, Socket, _dataClient.IP != null ? _dataClient : new DataClient(){IP = _ip});
                  _dataClient.ListHoldConnection.TryRemove(_data.Item5, out _);

                  switch(_data.Item3){
                     case TypeContent.Foreground:
                        if(_data.Item1.Length <= (limitMaxByteSize > 0 ? limitMaxByteSize : _data.Item1.Length))
                        if(_data.Item1.Length <= (ListLimitMaxByteSizeGroupID.TryGetValue(_data.Item2, out var _limitMaxByteSizeGroupID) ? _limitMaxByteSizeGroupID : _data.Item1.Length))
                        if(!ListLimitMaxPPSGroupID.TryGetValue(_data.Item2, out var _limitMaxPPSGroupdID) || _limitMaxPPSGroupdID.NotLimited)
                        if(Environment.TickCount >= _dataClient.PPS + (1000f / limitMaxPPS) || limitMaxPPS == 0){
                           _dataClient.PPS = Environment.TickCount;
                           packetsReceived += _data.Item1.Length;
                           Utility.RunOnMainThread(() => OnReceivedBytes?.Invoke(_data.Item1, _data.Item2, _dataClient));
                        }
                     break;
                     case TypeContent.Background:
                        if(_data.Item1.Length > 0)
                           switch(_data.Item4){
                              case TypeShipping.RSA:
                                 _dataClient = new DataClient() {IP = _ip, TimeLastPacket = Environment.TickCount, Time = Environment.TickCount, PublicKeyRSA = Encoding.ASCII.GetString(_data.Item1)};
                                 WaitDataClients.TryAdd(_ip, _dataClient);
                                 Send(Encoding.ASCII.GetBytes(Utility.PublicKeyRSAServer), 0, _dataClient, TypeShipping.RSA, TypeHoldConnection.NotEnqueue);
                              break;
                              case TypeShipping.AES:
                                 if(WaitDataClients.TryGetValue(_ip, out var _waitDataClient)){
                                    if(DataClients.TryAdd(_ip, _waitDataClient))
                                    if(WaitDataClients.TryRemove(_ip, out _)){
                                       Send(Utility.PrivateKeyAESServer, 1, _waitDataClient, TypeShipping.AES, TypeHoldConnection.NotEnqueue);
                                       _waitDataClient.PrivateKeyAES = _data.Item1;       // Client connected
                                       Utility.RunOnMainThread(() => OnConnectedClient?.Invoke(_waitDataClient));
                                       Utility.ShowLog(_waitDataClient.IP + " connected to the server.");
                                    }
                                 }
                              break;
                           }
                     break;
                  }
               }
            });
         }
      }

      static void CheckOnline(){
         while(Socket != null){
            Parallel.ForEach(DataClients.Values, _dataClient =>{
               _dataClient.PPS = 0;
               if(_dataClient.TimeLastPacket + 3000 < Environment.TickCount){
                  if(DataClients.TryRemove(_dataClient.IP, out _)){
                     Utility.RunOnMainThread(() => OnDisconnectedClient?.Invoke(_dataClient));
                     Utility.ShowLog(_dataClient.IP + " disconnected from the server.");
                  }
               }
            });

            Parallel.ForEach(WaitDataClients.Values, _dataClient =>{               
               if(_dataClient.TimeLastPacket + 10000 < Environment.TickCount)
                  WaitDataClients.TryRemove(_dataClient.IP, out _);
            });

            Parallel.ForEach(DataClients.Values, _dataClient =>{
               foreach(var _lhdc in _dataClient.ListHoldConnection){
                  if(!Utility.Send(Socket, _lhdc.Value.Bytes, _lhdc.Value.GroupID, _lhdc.Value.TypeShipping, _lhdc.Value.TypeHoldConnection, _lhdc.Value.TypeContent, _lhdc.Key, _dataClient))
                     lostPackets++;
                  lostPackets++;
               }
            });

            Thread.Sleep(1000);
         }
      }

      static void ChangeStatus(ServerStatusConnection _status){
         Utility.StartUnity();
         if(Status != _status){
            Status = _status;

            Utility.RunOnMainThread(() => OnServerStatus?.Invoke(Status));
            
            if(_status != ServerStatusConnection.Running){
               packetsCount = 0;
               packetsReceived = 0;
               packetsReceived2 = 0;
               packetsSent = 0;
               packetsSent2 = 0;
               lostPackets = 0;
            }
            switch(_status){
               case ServerStatusConnection.Initializing:
                  Utility.ShowLog("Initializing server...");
               break;
               case ServerStatusConnection.Running:
                  Utility.ShowLog("Server initialized and hosted on: " + host.ToString());
               break;
               case ServerStatusConnection.Restarting:
                  Utility.ShowLog("Restarting server...");
               break;
               case ServerStatusConnection.Stopping:
                  Utility.ShowLog("Stopping Server...");
               break;
               case ServerStatusConnection.Stopped:
                  Utility.ShowLog("Server stopped.");
               break;
            }
         }
      }
   }
}