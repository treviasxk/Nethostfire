// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nethostfire {
    public partial class UDP{
        public class Client {
            public static UdpClient Socket;
            static IPEndPoint host;
            static int packetsCount, pingCount, packetsTmp, timeTmp, connectTimeOut = 10000, lostPackets, pingTmp;
            static long timeLastPacket;
            static bool showUnityNetworkStatistics = false;
            static string publicKeyRSA = null;
            static byte[] privateKeyAES = null;
            static float packetsReceived, packetsReceived2, packetsSent, packetsSent2;
            static Thread SendOnlineThread, ClientReceiveUDPThread;

            /// <summary>
            /// OnReceivedBytes an event that returns bytes received and GroupID whenever the received bytes by clients, with it you can manipulate the bytes received.
            /// </summary>
            public static Action<byte[], int> OnReceivedBytes;
            /// <summary>
            /// OnClientStatus is an event that returns Client.ClientStatusConnection whenever the status changes, with which you can use it to know the current status of the server.
            /// </summary>
            public static Action<ClientStatusConnection> OnClientStatus;
            /// <summary>
            /// PublicKeyRSA returns the RSA public key obtained by the server after connecting.
            /// </summary>
            public static string PublicKeyRSA {get {return publicKeyRSA;}}
            /// <summary>
            /// PrivateKeyAES returns the AES private key obtained by the server after connecting.
            /// </summary>
            public static byte[] PrivateKeyAES {get {return privateKeyAES;}set{privateKeyAES = value;}}
            /// <summary>
            /// The Status is an enum Client.ClientStatusConnection with it you can know the current state of the client.
            /// </summary>
            public static ClientStatusConnection Status {get;set;} = ClientStatusConnection.Disconnected;
            /// <summary>
            /// PacketsPerSeconds is the amount of packets per second that happen when the client is sending and receiving.
            /// </summary>
            public static string PacketsPerSeconds {get {return packetsCount + "pps";}}
            /// <summary>
            /// ConnectTimeOut is the time the client will be reconnecting with the server, the time is defined in milliseconds, if the value is 0 the client will be reconnecting infinitely. The default value is 10000.
            /// </summary>
            public static int ConnectTimeOut {get {return connectTimeOut;} set{connectTimeOut = value;}}
            /// <summary>
            /// ReceiveAndSendTimeOut defines the timeout in milliseconds for sending and receiving, if any packet exceeds that sending the client will ignore the receiving or sending. The default and recommended value is 1000.
            /// </summary>
            public static int ReceiveAndSendTimeOut {get {return Utility.receiveAndSendTimeOut;} set{if(Socket != null){Socket.Client.ReceiveTimeout = value; Socket.Client.SendTimeout = value;} Utility.receiveAndSendTimeOut = value > 0 ? value : 1000;}}
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
            public static int Ping {get {return pingCount;}}
            /// <summary>
            /// Connect to a server with IP, Port and sets the size of SymmetricSizeRSA if needed.
            /// </summary>
            public static void Connect(IPAddress _ip, int _port, int _symmetricSizeRSA = 86){
                if(Socket is null){
                    Socket = new UdpClient();
                    Socket.Client.SendTimeout = Utility.receiveAndSendTimeOut;
                    Socket.Client.ReceiveTimeout = Utility.receiveAndSendTimeOut;
                    Utility.GenerateKey(TypeUDP.Client, _symmetricSizeRSA);
                    host = new IPEndPoint(_ip, _port);
                    try{
                        Socket.Connect(host);
                    }catch{
                        throw new Exception(Utility.ShowLog("Unable to connect to the server."));
                    }
                    timeLastPacket = Environment.TickCount;

                    if(ClientReceiveUDPThread == null){
                        ClientReceiveUDPThread = new Thread(ClientReceiveUDP)
                        {
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        };
                        ClientReceiveUDPThread.SetApartmentState(ApartmentState.MTA);
                        ClientReceiveUDPThread.Start();
                    }
                    if(SendOnlineThread == null){
                        SendOnlineThread = new Thread(SendOnline)
                        {
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        };
                        SendOnlineThread.SetApartmentState(ApartmentState.MTA);
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
                    Utility.SendPing(Socket, [0]);
                    Socket.Close();
                    Socket = null;
                    SendOnlineThread = null;
                    ClientReceiveUDPThread = null;
                    publicKeyRSA = null;
                    privateKeyAES = null;
                    Utility.listIndex.Clear();
                    Utility.listHoldConnectionClient.Clear();
                    if(Status == ClientStatusConnection.Disconnecting)
                        ChangeStatus(ClientStatusConnection.Disconnected);
                    if(Status == ClientStatusConnection.Connecting)
                        ChangeStatus(ClientStatusConnection.ConnectionFail);
                }
            }



            /// <summary>
            /// To send bytes to server, it is necessary to define the Bytes and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(byte[] _byte, int _groupID){
                PrepareSend(_byte, _groupID, TypeShipping.None, TypeHoldConnection.None);
            }
            /// <summary>
            /// To send bytes to server, it is necessary to define the Bytes and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
                PrepareSend(_byte, _groupID, _typeShipping, _typeHoldConnection);
            }
            /// <summary>
            /// To send bytes to server, it is necessary to define the Bytes and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(byte[] _byte, int _groupID, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
                PrepareSend(_byte, _groupID, _typeShipping, _typeHoldConnection);
            }



            /// <summary>
            /// To send string to server, it is necessary to define the Text and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(string _text, int _groupID){
                PrepareSend(Encoding.UTF8.GetBytes(_text), _groupID, TypeShipping.None, TypeHoldConnection.None);
            }
            /// <summary>
            /// To send string to server, it is necessary to define the Text and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(string _text, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
                PrepareSend(Encoding.UTF8.GetBytes(_text), _groupID, _typeShipping, _typeHoldConnection);
            }
            /// <summary>
            /// To send string to server, it is necessary to define the Text and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(string _text, int _groupID, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
                PrepareSend(Encoding.UTF8.GetBytes(_text), _groupID, _typeShipping, _typeHoldConnection);
            }



            /// <summary>
            /// To send float to server, it is necessary to define the Number and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(float _number, int _groupID){
                PrepareSend(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, TypeShipping.None, TypeHoldConnection.None);
            }
            /// <summary>
            /// To send float to server, it is necessary to define the Number and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(float _number, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
                PrepareSend(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _typeShipping, _typeHoldConnection);
            }
            /// <summary>
            /// To send float to server, it is necessary to define the Number and GroupID, the other sending resources such as TypeShipping and TypeHoldConnection are optional.
            /// </summary>
            public static void Send(float _number, int _groupID, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None, TypeShipping _typeShipping = TypeShipping.None){
                PrepareSend(Encoding.Unicode.GetBytes(_number.ToString()), _groupID, _typeShipping, _typeHoldConnection);
            }


            static void PrepareSend(byte[] _byte, int _groupID, TypeShipping _typeShipping = TypeShipping.None, TypeHoldConnection _typeHoldConnection = TypeHoldConnection.None){
                if(Status == ClientStatusConnection.Connected || Status == ClientStatusConnection.Connecting){
                    if(!Utility.Send(Socket, _byte, _groupID, _typeShipping, _typeHoldConnection, Status == ClientStatusConnection.Connected ? TypeContent.Foreground : TypeContent.Background, Utility.IndexShipping++))
                        lostPackets++;
                    packetsSent += _byte.Length;
                    packetsTmp++;
                }
            }

            private static async void ClientReceiveUDP(){
                while(Socket != null){
                    byte[] data  = null;
                    
                    try{
                        var receivedResult = await Socket.ReceiveAsync();
                        data  = receivedResult.Buffer;
                    }catch{}

                    if(data != null)
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
                            var _data = Utility.ByteToReceive(data, Socket);
                            Utility.listHoldConnectionClient.TryRemove(_data.Item5, out _);
                            switch(_data.Item3){
                                case TypeContent.Foreground:
                                    if(Status == ClientStatusConnection.Connected){
                                        packetsReceived += data.Length;
                                        Utility.RunOnMainThread(() => OnReceivedBytes?.Invoke(_data.Item1, _data.Item2));
                                    }
                                break;
                                case TypeContent.Background:
                                    if(Status == ClientStatusConnection.Connecting){
                                        if(_data.Item1.Length > 0)
                                        if(_data.Item3 == TypeContent.Background)
                                            switch(_data.Item4){
                                                case TypeShipping.RSA:
                                                    publicKeyRSA = Encoding.ASCII.GetString(_data.Item1);
                                                    Send(Utility.PrivateKeyAESClient, 1, TypeShipping.AES, TypeHoldConnection.NotEnqueue);
                                                break;
                                                case TypeShipping.AES:
                                                    privateKeyAES = _data.Item1;
                                                    if(publicKeyRSA != null)
                                                        ChangeStatus(ClientStatusConnection.Connected);
                                                break;
                                            }
                                    }
                                break;
                            }
                        }
                    });
                }
            }
            static void SendOnline(){
                while(Socket != null){
                    // Enviando byte 1 para o server, para dizer que está online
                    if(Status == ClientStatusConnection.Connected){
                        pingTmp = Environment.TickCount;
                        Utility.SendPing(Socket, [1]);
                    }
                    // Verificando se o ultimo ping com o server é de 3000ms
                    if(Environment.TickCount - timeLastPacket > 3000 && Status == ClientStatusConnection.Connected)
                        ChangeStatus(ClientStatusConnection.Connecting);

                    // Verificando o tempo de reconexão (connectTimeOut) se esgotou.
                    if(Status == ClientStatusConnection.Connecting && Environment.TickCount - timeLastPacket > 3000 + connectTimeOut && connectTimeOut != 0)
                        DisconnectServer();

                    // Enviando bytes que estão em Utility.listHoldConnectionClient
                    if(Status == ClientStatusConnection.Connected || Status == ClientStatusConnection.Connecting)
                        Parallel.ForEach(Utility.listHoldConnectionClient, item => {
                            if(!Utility.Send(Socket, item.Value.Bytes, item.Value.GroupID, item.Value.TypeShipping, item.Value.TypeHoldConnection, item.Value.TypeContent, item.Key))
                                lostPackets++;
                            lostPackets++;
                        });
                    Thread.Sleep(1000);
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

                    
                    Utility.RunOnMainThread(() => OnClientStatus?.Invoke(Status));

                    switch(_status){
                        case ClientStatusConnection.Connecting:
                            publicKeyRSA = null;
                            privateKeyAES = null;
                            Utility.listIndex.Clear();
                            Utility.listHoldConnectionClient.Clear();
                            timeLastPacket = Environment.TickCount;
                            Send(Encoding.ASCII.GetBytes(Utility.PublicKeyRSAClient), 0, TypeShipping.RSA, TypeHoldConnection.NotEnqueue);
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
        }
    }
}