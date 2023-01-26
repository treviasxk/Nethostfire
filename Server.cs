// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Nethostfire {
   public class Server {
      static IPEndPoint host;
      static UdpClient MyServer;
      static int packetsCount, packetsTmp, timeTmp, receiveAndSendTimeOut = 1000, symmetricSizeRSA, limitMaxPPS = 0, maxClients = 32, lostPackets, limitMaxByteSize = 0, unityBatchModeFrameRate = 60;
      static float packetsReceived, packetsReceived2, packetsSent, packetsSent2;
      static ManualResetEvent manualResetEvent = new ManualResetEvent(true);
      static ConcurrentDictionary<int, int> ListLimitMaxByteSizeGroupID = new ConcurrentDictionary<int, int>();
      static ConcurrentDictionary<int, int> ListLimitMaxPPSGroupID = new ConcurrentDictionary<int, int>();
      static ConcurrentDictionary<IPEndPoint, DataClient> DataClients = new ConcurrentDictionary<IPEndPoint, DataClient>();
      static ConcurrentDictionary<IPEndPoint, DataClient> WaitDataClients = new ConcurrentDictionary<IPEndPoint, DataClient>();
      static ConcurrentDictionary<IPEndPoint, long> ListBlockedIPs = new ConcurrentDictionary<IPEndPoint, long>();
      static ConcurrentDictionary<DataClient, HoldConnectionServer> listHoldConnection = new ConcurrentDictionary<DataClient, HoldConnectionServer>();
      static Thread CheckOnlineThread, ServerReceiveUDPThread;
      /// <summary>
      /// OnReceivedNewDataClient an event that returns bytes received, GroupID and DataClient whenever the received bytes by clients, with it you can manipulate the bytes received.
      /// </summary>
      public static Action<byte[], int, DataClient> OnReceivedNewDataClient;
      /// <summary>
      /// OnServerStatusConnection is an event that returns Server.ServerStatusConnection whenever the status changes, with which you can use it to know the current status of the server.
      /// </summary>
      public static Action<ServerStatusConnection> OnServerStatusConnection;
      /// <summary>
      /// OnConnectedClient is an event that you can use to receive the DataClient whenever a new client connected.
      /// </summary>
      public static Action<DataClient> OnConnectedClient;
      /// <summary>
      /// OnDisconnectedClient is an event that you can use to receive the DataClient whenever a new client disconnected.
      /// </summary>
      public static Action<DataClient> OnDisconnectedClient;
      /// <summary>
      /// The Status is an enum Server.ServerStatusConnection with it you can know the current state of the server.
      /// </summary>
      public static ServerStatusConnection Status {get;set;} = ServerStatusConnection.Stopped;
      /// <summary>
      /// ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the server will ignore the receiving or sending. The default and recommended value is 1000.
      /// </summary>
      public static int ReceiveAndSendTimeOut {get {return receiveAndSendTimeOut;} set{if(MyServer != null){MyServer.Client.ReceiveTimeout = value; MyServer.Client.SendTimeout = value;} receiveAndSendTimeOut = value > 0 ? value : 1000;}}
      /// <summary>
      /// The LimitMaxPacketsPerSeconds will change the maximum limit of Packets per seconds (PPS), if the packets is greater than the limit in 1 second, the server will not call the Server.OnReceivedNewDataClient event with the received bytes. The default value is 0 which is unlimited.
      /// </summary>
      public static int LimitMaxPacketsPerSeconds {get {return limitMaxPPS;} set{limitMaxPPS = value;}}
      /// <summary>
      /// The LimitMaxByteReceive will change the maximum limit of bytes that the server will read when receiving, if the packet bytes is greater than the limit, the server will not call the Server.OnReceivedNewDataClient event with the received bytes. The default value is 0 which is unlimited.
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
      public static string PacketsPerSeconds {get {return packetsCount +"pps";}}
      /// <summary>
      /// PacketsBytesReceived is the amount of bytes received by the server.
      /// </summary>
      public static string PacketsBytesReceived {get {return Utility.BytesToString(packetsReceived2);}}
      /// <summary>
      /// The UnityBatchModeFrameRate limits the fps at which the dedicated server build (batchmode) will run, it is recommended to limit it to prevent the CPU from being used to the maximum. The default value is 60.
      /// </summary>
      public static int UnityBatchModeFrameRate {get {return unityBatchModeFrameRate;} set { unityBatchModeFrameRate = value;}}
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
      public static void Start(IPEndPoint _host = null, int _symmetricSizeRSA = 86){
         if(Status == ServerStatusConnection.Stopped || Status == ServerStatusConnection.Restarting)
            ChangeStatus(ServerStatusConnection.Initializing);
         if(MyServer is null && _host is not null){
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
               Utility.Process.PriorityClass = ProcessPriorityClass.High;
            MyServer = new UdpClient();
            MyServer.Client.SendTimeout = receiveAndSendTimeOut;
            MyServer.Client.ReceiveTimeout = receiveAndSendTimeOut;
            symmetricSizeRSA = _symmetricSizeRSA;
            Utility.GenerateKeyRSA(_symmetricSizeRSA);
            host = _host;
            try{
               MyServer.Client.Bind(_host);
            }catch{
               throw new Exception(Utility.ShowLog("Could not start the server, check that the port "+ _host.Port + " is not blocked, or that you have other software using that port."));
            }
            if(ServerReceiveUDPThread == null){
               ServerReceiveUDPThread = new Thread(ServerReceiveUDP);
               ServerReceiveUDPThread.IsBackground = true;
               ServerReceiveUDPThread.Start();
            }
            if(CheckOnlineThread == null){
               CheckOnlineThread = new Thread(CheckOnline);
               CheckOnlineThread.IsBackground = true;
               CheckOnlineThread.Start();
            }
         }else
            if(_host is null)
               throw new Exception(Utility.ShowLog("It is not possible to start the server, without the _host having been configured beforehand."));
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
            MyServer.Close();
            MyServer = null;
            ServerReceiveUDPThread = null;
            CheckOnlineThread = null;
            OnConnectedClient = null;
            OnDisconnectedClient = null;
            OnReceivedNewDataClient = null;
            listHoldConnection.Clear();
            DataClients.Clear();
            WaitDataClients.Clear();
            Utility.GenerateKeyRSA(symmetricSizeRSA);
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
            Start(host);
         }else
            throw new Exception(Utility.ShowLog("It is not possible to restart the server if it is not running."));
      }
      /// <summary>
      /// To send bytes to a client, it is necessary to define the bytes, GroupID and DataClient, the other sending resources such as TypeShipping and HoldConnection are optional.
      /// </summary>
      public static void SendBytes(byte[] _byte, int _groupID, DataClient _dataClient, TypeShipping _typeShipping = TypeShipping.None, bool _holdConnection = false){
         if(Status == ServerStatusConnection.Running){
            if(_holdConnection){
               if(listHoldConnection.TryGetValue(_dataClient, out var HoldConnection)){
                  HoldConnection.Time.Add(0);
                  HoldConnection.Bytes.Add(_byte);
                  HoldConnection.GroupID.Add(_groupID);
               }
               else
                  listHoldConnection.TryAdd(_dataClient, new HoldConnectionServer{Bytes = new List<byte[]>{_byte}, Time = new List<int>{0}, GroupID = new List<int>{_groupID}});
            }
            if(!Utility.Send(MyServer, _byte, _groupID, _typeShipping, _holdConnection, TypeContent.Foreground, _dataClient))
               lostPackets++;
            else
               packetsSent += _byte.Length;
            packetsTmp++;
         }
      }
      /// <summary>
      /// To send bytes to a group client, it is necessary to define the bytes, GroupID and List DataClient, the other sending resources such as TypeShipping, SkipDataClient and HoldConnection are optional.
      /// </summary>
      public static void SendBytesGroup(byte[] _byte, int _groupID, List<DataClient> _dataClients, TypeShipping _typeShipping = TypeShipping.None, DataClient _skipDataClient = null, bool _holdConnection = false){
         Parallel.ForEach(_dataClients.Where(item => item.IP != (_skipDataClient != null ? _skipDataClient.IP: null)), _dataClient => {
            if(Status == ServerStatusConnection.Running){
               if(_holdConnection){
                  if(listHoldConnection.TryGetValue(_dataClient, out var HoldConnection)){
                     HoldConnection.Time.Add(0);
                     HoldConnection.Bytes.Add(_byte);
                     HoldConnection.GroupID.Add(_groupID);
                  }
                  else
                     listHoldConnection.TryAdd(_dataClient, new HoldConnectionServer{Bytes = new List<byte[]>{_byte}, Time = new List<int>{0}, GroupID = new List<int>{_groupID}});
               }
               if(!Utility.Send(MyServer, _byte, _groupID, _typeShipping, _holdConnection, TypeContent.Foreground, _dataClient))
                  lostPackets++;
               else
                  packetsSent += _byte.Length;
               packetsTmp++;
            }
         });
      }
      /// <summary>
      /// To send bytes to all clients, it is necessary to define the bytes, GroupID, the other sending resources such as TypeShipping, SkipDataClient and HoldConnection are optional.
      /// </summary>
      public static void SendBytesAll(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, DataClient _skipDataClient = null, bool _holdConnection = false){
         Parallel.ForEach(DataClients.Where(item => item.Value.IP != (_skipDataClient != null ? _skipDataClient.IP: null)), item => {
            if(Status == ServerStatusConnection.Running){
               if(_holdConnection){
                  if(listHoldConnection.TryGetValue(item.Value, out var HoldConnection)){
                     HoldConnection.Time.Add(0);
                     HoldConnection.Bytes.Add(_byte);
                     HoldConnection.GroupID.Add(_groupID);
                  }
                  else
                     listHoldConnection.TryAdd(item.Value, new HoldConnectionServer{Bytes = new List<byte[]>{_byte}, Time = new List<int>{0}, GroupID = new List<int>{_groupID}});
               }
               if(!Utility.Send(MyServer, _byte, _groupID, _typeShipping, _holdConnection, TypeContent.Foreground, item.Value))
                  lostPackets++;
               else
                  packetsSent += _byte.Length;
               packetsTmp++;
            }
         });
      }
      /// <summary>
      /// To disconnect a client from server, it is necessary to inform the DataClient.
      /// </summary>
      public static void DisconnectClient(DataClient _dataClient){
         if(Status == ServerStatusConnection.Running)
            Utility.SendPing(MyServer, new byte[]{0},_dataClient);
      }
      /// <summary>
      /// To disconnect a group clients from server, it is necessary to inform the List DataClient.
      /// </summary>
      public static void DisconnectClientGroup(ConcurrentQueue<DataClient> _dataClients){
         Parallel.ForEach(_dataClients, dataClient => {
            if(Status == ServerStatusConnection.Running)
               Utility.SendPing(MyServer, new byte[]{0}, dataClient);
         });
      }
      /// <summary>
      /// To disconnect alls clients from server.
      /// </summary>
      public static void DisconnectClientAll(){
         Parallel.ForEach(DataClients, item => {
            if(Status == ServerStatusConnection.Running)
               Utility.SendPing(MyServer, new byte[]{0}, item.Value);
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
            Utility.SendPing(MyServer, new byte[]{2}, new DataClient(){IP = _ip});
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
                  Utility.SendPing(MyServer, new byte[]{2}, _dataClient);
               
               if(WaitDataClients.TryRemove(_ip, out var _waitDataClient))
                  Utility.SendPing(MyServer, new byte[]{2}, _waitDataClient);
               
               return true;
            } 
         }else
            return false;
      }


      /// <summary>
      /// The ChangeLimitMaxByteSizeGroupID will change the maximum limit of bytes of a GroupID that the server will read when receiving the bytes, if the packet bytes is greater than the limit, the server will not call the Server.OnReceivedNewDataClient event with the received bytes. The default value _limitBytes is 0 which is unlimited.
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
      /// The ChangeLimitMaxPacketsPerSecondsGroupID will change the maximum limit of Packets per seconds (PPS) of a GroupID, if the packets is greater than the limit in 1 second, the server will not call the Server.OnReceivedNewDataClient event with the received bytes. The default value is _limitBytes 0 which is unlimited.
      /// </summary>
      public static void ChangeLimitMaxPacketsPerSecondsGroupID(int _groupID, int _limitPPS){
         if(ListLimitMaxPPSGroupID.TryGetValue(_groupID, out var _limitMaxPPGroupID)){
            if(_limitPPS < 0)
               _limitMaxPPGroupID = _limitPPS;
            else
               ListLimitMaxPPSGroupID.TryRemove(_groupID, out _);
         }else
            ListLimitMaxPPSGroupID.TryAdd(_groupID, _limitPPS);
      }


      static void ServerReceiveUDP(){
         while(MyServer != null){
            byte[] data = null;
            IPEndPoint _ip = null;
            try{
               data = MyServer.Receive(ref _ip);
            }catch{}
            if(data != null && !CheckBlockerIP(_ip)){
               Parallel.Invoke(()=>{
                  packetsTmp++;
                  if(DateTime.Now.Second != timeTmp){
                     timeTmp = DateTime.Now.Second;
                     packetsReceived2 = packetsReceived;
                     packetsSent2 = packetsSent;
                     packetsCount = packetsTmp;
                     packetsTmp = 0;
                  }

                  DataClient _dataClient = new DataClient();
                  if(DataClients.TryGetValue(_ip, out var _client)){
                     _dataClient = _client;
                     _dataClient.TimeLastPacket = Environment.TickCount;
                     if(data.Length == 1){
                        switch(data[0]){
                           case 0:
                              listHoldConnection.TryRemove(_dataClient, out _);
                              Utility.RunOnMainThread(() => OnDisconnectedClient?.Invoke(_dataClient));
                              DataClients.TryRemove(_ip, out _);
                              Utility.ShowLog(_dataClient.IP + " disconnected from the server.");
                           break;
                           case 1:
                              _dataClient.Ping = Environment.TickCount - _dataClient.Time - 1000;
                              _dataClient.Time = Environment.TickCount;
                              Utility.SendPing(MyServer, new byte[]{1},_dataClient);
                           break;
                        }
                     }
                  }else{
                     if(DataClients.Count >= maxClients){
                        Utility.SendPing(MyServer, new byte[]{3}, new DataClient(){IP = _ip});
                        data = new byte[]{};
                     }
                  }

                  if(data.Length > 1 && Status == ServerStatusConnection.Running){
                     var _data = Utility.ByteToReceive(data, MyServer, _dataClient);
                     if(listHoldConnection.TryGetValue(_dataClient, out var _holdConnection)){
                        if(_holdConnection.GroupID.Contains(_data.Item2)){
                           var index = _holdConnection.GroupID.IndexOf(_data.Item2);
                           _holdConnection.Bytes.RemoveAt(index);
                           _holdConnection.Time.RemoveAt(index);
                           _holdConnection.GroupID.RemoveAt(index);
                           if(_holdConnection.GroupID.Count == 0)
                              listHoldConnection.TryRemove(_dataClient, out _);
                        }
                     }

                     if(_data.Item1.Length != 0){
                        if(_data.Item3 == TypeContent.Background){
                           switch(_data.Item4){
                              case TypeShipping.RSA:
                                 _dataClient = new DataClient() {IP = _ip, TimeLastPacket = Environment.TickCount, Time = Environment.TickCount, PublicKeyRSA = Encoding.ASCII.GetString(_data.Item1)};
                                 WaitDataClients.TryAdd(_ip, _dataClient);
                                 Utility.Send(MyServer, Encoding.ASCII.GetBytes(Utility.PublicKeyRSA), 0, TypeShipping.RSA, false, TypeContent.Background, _dataClient);
                              break;
                              case TypeShipping.AES:
                                 if(WaitDataClients.TryGetValue(_ip, out var _waitDataClient)){
                                    _waitDataClient.PrivateKeyAES = _data.Item1;
                                    if(DataClients.TryAdd(_ip, _waitDataClient))
                                    if(WaitDataClients.TryRemove(_ip, out _)){
                                       Utility.Send(MyServer, Utility.PrivateKeyAES, 1, TypeShipping.AES, false, TypeContent.Background, _waitDataClient);
                                       Utility.RunOnMainThread(() => OnConnectedClient?.Invoke(_waitDataClient));
                                       Utility.ShowLog(_waitDataClient.IP + " connected to the server.");
                                    }
                                 }
                              break;
                           }
                        }else
                        if(_data.Item1.Length <= (limitMaxByteSize > 0 ? limitMaxByteSize : _data.Item1.Length))
                        if(_data.Item1.Length <= (ListLimitMaxByteSizeGroupID.TryGetValue(_data.Item2, out var _limitMaxByteSizeGroupID) ? _limitMaxByteSizeGroupID : _data.Item1.Length))
                        if(_dataClient.PPS <= (ListLimitMaxPPSGroupID.TryGetValue(_data.Item2, out var _limitMaxPPSGroupdID) ? _limitMaxPPSGroupdID - 1 : _dataClient.PPS))
                        if(_dataClient.PPS <= (limitMaxPPS > 0 ? limitMaxPPS - 1 : _dataClient.PPS)){
                           _dataClient.PPS++;
                           packetsReceived += _data.Item1.Length;
                           if(listHoldConnection.TryGetValue(_dataClient, out var _holdConnection2)){
                              if(_holdConnection2.GroupID.Contains(_data.Item2)){
                                 var index = _holdConnection2.GroupID.IndexOf(_data.Item2);
                                 if(_holdConnection2.Time[index] == 0){
                                    _holdConnection2.Time[index] = Environment.TickCount + receiveAndSendTimeOut;
                                    Utility.RunOnMainThread(() => OnReceivedNewDataClient?.Invoke(_data.Item1, _data.Item2, _dataClient));
                                 }else{
                                    if(_holdConnection2.Time[index] < Environment.TickCount)
                                       listHoldConnection.TryRemove(_dataClient, out _);
                                 }
                              }
                           }else{
                              Utility.RunOnMainThread(() => OnReceivedNewDataClient?.Invoke(_data.Item1, _data.Item2, _dataClient));
                           }
                        }
                     }else
                        lostPackets++;
                  }
               });
            }
            manualResetEvent.WaitOne();
         }
      }

      static void CheckOnline(){
         while(MyServer != null){
            Utility.StopThreadUnity();

            Parallel.ForEach(DataClients, item =>{
               item.Value.PPS = 0;
               if(item.Value.TimeLastPacket + 3000 < Environment.TickCount){
                  if(DataClients.TryRemove(item.Value.IP, out var _dataClient)){
                     listHoldConnection.TryRemove(item.Value, out _);
                     Utility.RunOnMainThread(() => OnDisconnectedClient?.Invoke(item.Value));
                     Utility.ShowLog(_dataClient.IP + " disconnected from the server.");
                  }
               }

               if(Status == ServerStatusConnection.Running){
                  Parallel.ForEach(listHoldConnection, item =>{
                     for(int i = 0; i < item.Value.GroupID.Count; i++){
                        if(!Utility.Send(MyServer, item.Value.Bytes[i], item.Value.GroupID[i], TypeShipping.None, true, TypeContent.Foreground, item.Key))
                           lostPackets++;
                        lostPackets++;
                     }
                  });
               }
               manualResetEvent.WaitOne();
            });   
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(1000);
         }
      }

      static void ChangeStatus(ServerStatusConnection _status){
         Utility.StartUnity();
         if(Status != _status){
            Status = _status;
            Thread t = new Thread(new ThreadStart(NewThreadStatus));
            t.Start();
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

      static void NewThreadStatus(){
         Utility.RunOnMainThread(() => OnServerStatusConnection?.Invoke(Status));
      }
   }
}