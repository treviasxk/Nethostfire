// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace Nethostfire {
    public class Client {
        static UdpClient MyClient;
        static IPEndPoint host;
        static int packetsCount, pingCount, packetsTmp, timeTmp, connectTimeOut = 10000, receiveAndSendTimeOut = 1000, lostPackets, pingTmp;
        static long timeLastPacket;
        static bool showUnityNetworkStatistics = false;
        static string publicKeyRSA = null;
        static float packetsReceived, packetsReceived2, packetsSent, packetsSent2;
        static ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        static ConcurrentDictionary<int, HoldConnectionClient> listHoldConnection = new ConcurrentDictionary<int, HoldConnectionClient>();
        static Thread SendOnlineThread, ClientReceiveUDPThread;
        /// <summary>
        /// OnReceivedNewDataServer an event that returns bytes received and GroupID whenever the received bytes by clients, with it you can manipulate the bytes received.
        /// </summary>
        public static Action<byte[], int> OnReceivedNewDataServer;
        /// <summary>
        /// OnClientStatusConnection is an event that returns Client.ClientStatusConnection whenever the status changes, with which you can use it to know the current status of the server.
        /// </summary>
        public static Action<ClientStatusConnection> OnClientStatusConnection;
        /// <summary>
        /// PublicKeyRSA returns the RSA public key obtained by the server after connecting.
        /// </summary>
        public static string PublicKeyRSA {get {return publicKeyRSA;}}
        /// <summary>
        /// PrivateKeyAES returns the AES private key obtained by the server after connecting.
        /// </summary>
        public static byte[] PrivateKeyAES {get;set;}
        /// <summary>
        /// The Status is an enum Client.ClientStatusConnection with it you can know the current state of the client.
        /// </summary>
        public static ClientStatusConnection Status {get;set;} = ClientStatusConnection.Disconnected;
        /// <summary>
        /// PacketsPerSeconds is the amount of packets per second that happen when the client is sending and receiving.
        /// </summary>
        public static string PacketsPerSeconds {get {return packetsCount +"pps";}}
        /// <summary>
        /// ConnectTimeOut is the time the client will be reconnecting with the server, the time is defined in milliseconds, if the value is 0 the client will be reconnecting infinitely. The default value is 10000.
        /// </summary>
        public static int ConnectTimeOut {get {return connectTimeOut;} set{connectTimeOut = value;}}
        /// <summary>
        /// ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the client will ignore the receiving or sending. The default and recommended value is 1000.
        /// </summary>
        public static int ReceiveAndSendTimeOut {get {return receiveAndSendTimeOut;} set{if(MyClient != null){MyClient.Client.ReceiveTimeout = value; MyClient.Client.SendTimeout = value;} receiveAndSendTimeOut = value > 0 ? value : 1000;}}
        /// <summary>
        /// PacketsBytesReceived is the amount of bytes received by the client.
        /// </summary>
        public static string PacketsBytesReceived {get {return Utility.BytesToString(packetsReceived2);}}
        /// <summary>
        /// PacketsBytesSent is the amount of bytes sent by the client.
        /// </summary>
        public static string PacketsBytesSent {get {return Utility.BytesToString(packetsSent2);}}
        /// <summary>
        /// LostPackets is the number of packets lost.
        /// </summary>
        public static int LostPackets {get {return lostPackets;}}
        /// <summary>
        /// The ShowDebugConsole when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. The default value is true.
        /// </summary>
        public static bool ShowDebugConsole {get {return Utility.ShowDebugConsole;} set { Utility.ShowDebugConsole = value;}}
        /// <summary>
        /// When using Nethostfire in Unity and when set the value of ShowUnityNetworkStatistics to true, statistics on the connection between the client and the server will be displayed during game execution.
        /// </summary>
        public static bool ShowUnityNetworkStatistics {get {return showUnityNetworkStatistics;} set {showUnityNetworkStatistics = value;}}
        /// <summary>
        /// Ping returns an integer value, this value is per milliseconds
        /// </summary>
        public static int Ping {get {return (int)pingCount;}}
        /// <summary>
        /// Connect to a server with IP, Port and sets the size of SymmetricSizeRSA if needed.
        /// </summary>
        public static void Connect(IPEndPoint _host, int _symmetricSizeRSA = 86){
            if(MyClient is null){
                MyClient = new UdpClient();
                MyClient.Client.SendTimeout = receiveAndSendTimeOut;
                MyClient.Client.ReceiveTimeout = receiveAndSendTimeOut;
                Utility.GenerateKeyRSA(_symmetricSizeRSA);
                host = _host;
                try{
                    MyClient.Connect(_host);
                }catch{
                    throw new Exception(Utility.ShowLog("Unable to connect to the server."));
                }
                timeLastPacket = Environment.TickCount;

                if(ClientReceiveUDPThread == null){
                    ClientReceiveUDPThread = new Thread(ClientReceiveUDP);
                    ClientReceiveUDPThread.IsBackground = true;
                    ClientReceiveUDPThread.Start();
                }
                if(SendOnlineThread == null){
                    SendOnlineThread = new Thread(SendOnline);
                    SendOnlineThread.IsBackground = true;
                    SendOnlineThread.Start();
                }                    
            }
            //manualResetEvent.Set();
            if(Status != ClientStatusConnection.Connected)
                ChangeStatus(ClientStatusConnection.Connecting);
            else
                Utility.StartUnity();
        }
        /// <summary>
        /// With DisconnectServer the client will be disconnected from the server.
        /// </summary>
        public static void DisconnectServer(){
            if(Status == ClientStatusConnection.Connected || Status == ClientStatusConnection.Connecting){
                if(Status == ClientStatusConnection.Connected)
                    ChangeStatus(ClientStatusConnection.Disconnecting);
                Utility.SendPing(MyClient, new byte[]{0});
                MyClient.Close();
                MyClient = null;
                SendOnlineThread = null;
                ClientReceiveUDPThread = null;
                OnClientStatusConnection = null;
                OnReceivedNewDataServer = null;
                publicKeyRSA = null;
                PrivateKeyAES = null;
                listHoldConnection.Clear();
                if(Status == ClientStatusConnection.Disconnecting)
                    ChangeStatus(ClientStatusConnection.Disconnected);
                if(Status == ClientStatusConnection.Connecting)
                    ChangeStatus(ClientStatusConnection.ConnectionFail);
            }
        }
        /// <summary>
        /// To send bytes to server, it is necessary to define the bytes and GroupID, the other sending resources such as TypeShipping and HoldConnection are optional.
        /// </summary>
        public static void SendBytes(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, bool _holdConnection = false){
            if(Status == ClientStatusConnection.Connected || Status == ClientStatusConnection.Connecting){
                if(_holdConnection){
                    if(listHoldConnection.TryGetValue(_groupID, out var HoldConnection)){
                        HoldConnection.Time = Environment.TickCount + receiveAndSendTimeOut;
                        HoldConnection.Bytes = _byte;
                        HoldConnection.TypeShipping = _typeShipping;
                        HoldConnection.TypeContent = (Status == ClientStatusConnection.Connected ? TypeContent.Foreground : TypeContent.Background);
                    }else
                        listHoldConnection.TryAdd(_groupID, new HoldConnectionClient{Bytes = _byte, Time = 0, TypeShipping = _typeShipping, TypeContent = Status == ClientStatusConnection.Connected ? TypeContent.Foreground : TypeContent.Background});
                }
                if(!Utility.Send(MyClient, _byte, _groupID, _typeShipping, _holdConnection, (Status == ClientStatusConnection.Connected ? TypeContent.Foreground : TypeContent.Background)))
                    lostPackets++;
                packetsSent += _byte.Length;
                packetsTmp++;
            }
        }

        private static void ClientReceiveUDP(){
            while(MyClient != null){
                byte[] data = null;
                IPEndPoint _host = null;
                try{
                    data = MyClient.Receive(ref _host);
                }catch{}

                if(data != null && _host.Equals(host)){
                    Parallel.Invoke(()=>{
                        timeLastPacket = Environment.TickCount;
                        packetsTmp++;
                        if(DateTime.Now.Second != timeTmp){
                            timeTmp = DateTime.Now.Second;
                            packetsSent2 = packetsSent;
                            packetsReceived2 = packetsReceived;
                            packetsCount = packetsTmp;
                            packetsTmp = 0;
                        }

                        if(data.Length == 1){
                            switch(data[0]){
                                case 0:
                                    ChangeStatus(ClientStatusConnection.Disconnected);
                                break;
                                case 1:
                                    pingCount = Environment.TickCount - pingTmp;
                                break;
                                case 2:
                                    ChangeStatus(ClientStatusConnection.IpBlocked);
                                break;
                                case 3:
                                    ChangeStatus(ClientStatusConnection.MaxClientExceeded);
                                break;
                            }
                        }

                        if(data.Length > 1){
                            var _data = Utility.ByteToReceive(data, MyClient);
                            if(!listHoldConnection.TryRemove(_data.Item2, out _) && _data.Item1.Length == 0)
                                lostPackets++;
                            if(_data.Item1.Length > 0){
                                if(Status == ClientStatusConnection.Connecting){
                                    if(_data.Item3 == TypeContent.Background)
                                        switch(_data.Item4){
                                            case TypeShipping.RSA:
                                                publicKeyRSA = Encoding.ASCII.GetString(_data.Item1);
                                                SendBytes(Utility.PrivateKeyAES, 1, TypeShipping.AES, true);
                                            break;
                                            case TypeShipping.AES:
                                                PrivateKeyAES = _data.Item1;
                                                if(publicKeyRSA != null)
                                                    ChangeStatus(ClientStatusConnection.Connected);
                                            break;
                                        }
                                }else{
                                    packetsReceived += _data.Item1.Length;
                                    Utility.RunOnMainThread(() => OnReceivedNewDataServer?.Invoke(_data.Item1, _data.Item2));
                                }
                            }
                        }
                    });
                }
                manualResetEvent.WaitOne();
            }
        }
        static void SendOnline(){
            while(MyClient != null){
                Utility.StopThreadUnity();
            
                // Enviando byte 1 para o server, para dizer que está online
                if(Status == ClientStatusConnection.Connected){
                    pingTmp = Environment.TickCount;
                    Utility.SendPing(MyClient, new byte[]{1});
                }

                // Verificando se o ultimo ping com o server é de 3000ms
                if(Environment.TickCount - timeLastPacket > 3000 && Status == ClientStatusConnection.Connected)
                    ChangeStatus(ClientStatusConnection.Connecting);

                // Verificando o tempo de reconexão (connectTimeOut) se esgotou.
                if(Status == ClientStatusConnection.Connecting && Environment.TickCount - timeLastPacket > 3000 + connectTimeOut && connectTimeOut != 0)
                    DisconnectServer();

                // Enviando bytes que estão em listHoldConnection
                if(Status == ClientStatusConnection.Connected || Status == ClientStatusConnection.Connecting)
                    try{
                        foreach(var item in listHoldConnection.ToArray()){
                            if(item.Value.Time < Environment.TickCount){
                                item.Value.Time = Environment.TickCount + receiveAndSendTimeOut;
                                if(!Utility.Send(MyClient, item.Value.Bytes, item.Key, item.Value.TypeShipping, true, item.Value.TypeContent))
                                    lostPackets++;
                                lostPackets++;
                            }
                        }
                    }catch{}
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Thread.Sleep(1000);
                manualResetEvent.WaitOne();
            }
        }

        static void ChangeStatus(ClientStatusConnection _status){
            Utility.StartUnity();
            if(Status != _status){
                Status = _status;
                if(_status != ClientStatusConnection.Connected){
                    pingCount = 0;
                    packetsCount = 0;
                    packetsReceived = 0;
                    packetsReceived2 = 0;
                    packetsSent = 0;
                    packetsSent2 = 0;
                    lostPackets = 0;
                }
                Thread t = new Thread(new ThreadStart(NewThreadStatus));
                t.Start();
                switch(_status){
                    case ClientStatusConnection.Connecting:
                        publicKeyRSA = null;
                        PrivateKeyAES = null;
                        listHoldConnection.Clear();
                        timeLastPacket = Environment.TickCount;
                        SendBytes(Encoding.ASCII.GetBytes(Utility.PublicKeyRSA), 0, TypeShipping.RSA, true);
                        Utility.ShowLog("Connecting on " + host);
                    break;
                    case ClientStatusConnection.Connected:
                        Utility.ShowLog("Connected!");
                    break;
                    case ClientStatusConnection.ConnectionFail:
                        Utility.ShowLog("Unable to connect to the server.");
                    break;
                    case ClientStatusConnection.Disconnecting:
                        Utility.ShowLog("Disconnecting...");
                    break;
                    case ClientStatusConnection.Disconnected:
                        Utility.ShowLog("Disconnected!");
                    break;
                    case ClientStatusConnection.IpBlocked:
                        Utility.ShowLog("Your IP has been blocked by the server!");
                    break;
                    case ClientStatusConnection.MaxClientExceeded:
                        Utility.ShowLog("The maximum number of connected clients has been exceeded!");
                    break;
                }
            }
        }

        static void NewThreadStatus(){
            Utility.RunOnMainThread(() => OnClientStatusConnection?.Invoke(Status));
        }
    }
}