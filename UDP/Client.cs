// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.Utility;

namespace Nethostfire {
    public partial class UDP{
        public class Client : IDisposable{
            public UdpClient? Socket;
            IPEndPoint? IPEndPoint;
            DataClient dataServer = new();
            ClientStatus CurrentClientStatus = ClientStatus.Disconnected;
            long connectingTimeoutTmp;

            /// <summary>
            /// ConnectTimeout is the maximum time limit in milliseconds that a client can remain connected to the server when a ping is not received. (default value is 3000).
            /// </summary>
            public int ConnectTimeout {get; set;} = 3000;

            /// <summary>
            /// ConnectingTimeout is the time the client will be reconnecting with the server, the time is defined in milliseconds, if the value is 0 the client will be reconnecting infinitely. (The default value is 10000).
            /// </summary>
            public int ConnectingTimeout {get; set;} = 10000;

            /// <summary>
            /// The ShowLogDebug when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
            /// </summary>
            public bool ShowLogDebug {get; set;} = true;

            /// <summary>
            /// Ping returns an integer value, this value is per milliseconds
            /// </summary>
            public int Ping {get {return dataServer.Ping;}}

            /// <summary>
            /// OnStatus is an event that returns ClientStatus whenever the status changes, with which you can use it to know the current status of the client.
            /// </summary>
            public Action<ClientStatus>? OnStatus;

            /// <summary>
            /// OnReceivedBytes an event that returns bytes received and GroupID whenever the received bytes by server, with it you can manipulate the bytes received.
            /// </summary>
            public Action<byte[], int>? OnReceivedBytes;

            /// <summary>
            /// The Status is an enum ClientStatus with it you can know the current state of the client.
            /// </summary>
            public ClientStatus Status {get{return CurrentClientStatus;}} 

            /// <summary>
            /// Connect to a server with IP, Port and sets the size of SymmetricSizeRSA if needed.
            /// </summary>
            public void Connect(IPAddress ip, int port, int symmetricSizeRSA = 86){
                if(Socket == null){
                    StartUnity(client: this);
                    Socket = new UdpClient();
                    GenerateKey(symmetricSizeRSA);
                    try{
                        IPEndPoint = new IPEndPoint(ip, port);
                        ChangeStatus(ClientStatus.Connecting);
                        Socket.Connect(IPEndPoint);
                        new Thread(ReceivePackage){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                        new Thread(Service){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                    }catch{
                        ChangeStatus(ClientStatus.ConnectionFail);
                    }
                }
            }


            /// <summary>
            /// With Disconnect the client will be disconnected from the server.
            /// </summary>
            public void Disconnect(){
                if(Socket != null){
                    ChangeStatus(ClientStatus.Disconnecting);
                    Socket.Close();
                    Socket = null;
                    dataServer = new();
                    ChangeStatus(ClientStatus.Disconnected);
                }
            }

            
            /// <summary>
            /// To send bytes to server, it is necessary to define the Bytes and GroupID, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(byte[] bytes, int groupID) => SendPrepare(bytes, groupID);
            /// <summary>
            /// To send bytes to server, it is necessary to define the Bytes and GroupID, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(byte[] bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPrepare(bytes, groupID, typeEncrypt, typeShipping);
            /// <summary>
            /// To send bytes to server, it is necessary to define the Bytes and GroupID, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void Send(byte[] bytes, int groupID, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendPrepare(bytes, groupID, typeEncrypt, typeShipping);

            /// <summary>
            /// To send string to server, it is necessary to define the Text and GroupID, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(string text, int groupID) => SendPrepare(text, groupID);
            /// <summary>
            /// To send string to server, it is necessary to define the Text and GroupID, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPrepare(text, groupID, typeEncrypt, typeShipping);
            /// <summary>
            /// To send string to server, it is necessary to define the Text and GroupID, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void Send(string text, int groupID, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendPrepare(text, groupID, typeEncrypt, typeShipping);

            /// <summary>
            /// To send float to server, it is necessary to define the Value and GroupID, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(float value, int groupID) => SendPrepare(value, groupID);
            /// <summary>
            /// To send float to server, it is necessary to define the Value and GroupID, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(float value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPrepare(value, groupID, typeEncrypt, typeShipping);
            /// <summary>
            /// To send float to server, it is necessary to define the Value and GroupID, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void Send(float value, int groupID, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendPrepare(value, groupID, typeEncrypt, typeShipping);


            void SendPrepare(byte[] bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ClientStatus.Connected && bytes != null)
                    SendPacket(Socket, bytes, groupID, dataServer, typeEncrypt, typeShipping);
            }

            void SendPrepare(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ClientStatus.Connected && text != null)
                    SendPacket(Socket, Encoding.UTF8.GetBytes(text), groupID, dataServer, typeEncrypt, typeShipping);
            }

            void SendPrepare(float value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ClientStatus.Connected)
                    SendPacket(Socket, BitConverter.GetBytes(value), groupID, dataServer, typeEncrypt, typeShipping);
            }


            async void ReceivePackage(){
                while(Socket != null){
                    byte[] bytes;

                    // Connection alway fail when ip not found, use 'try' is necessary.
                    try{
                        // Receive bytes
                        var receivedResult = await Socket.ReceiveAsync();
                        bytes = receivedResult.Buffer;
                    }catch{
                        continue;
                    }
                    
                    Parallel.Invoke(()=>{
                        // Update ping and timer connection
                        if(bytes.Length == 1){
                            switch(bytes[0]){
                                case 0: // Disconnect from server
                                    Disconnect();
                                return;
                                case 1: // Update ping
                                    long Timer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                    dataServer.Ping = Convert.ToInt32(Timer - dataServer.LastTimer);
                                    dataServer.LastTimer = Timer;
                                return;
                                case 2: // Max client exceeded
                                    ChangeStatus(ClientStatus.MaxClientExceeded);
                                return;
                                case 3: // IP blocked
                                    ChangeStatus(ClientStatus.IpBlocked);
                                return;
                            }
                        }
                        
                        // item1 = bytes
                        // item2 = groupID
                        // item3 = typeEncrypt
                        // item4 = typeShipping
                        var data = BytesToReceive(Socket, bytes, dataServer);
                        if(data.HasValue)
                        if(Status == ClientStatus.Connected){
                            RunOnMainThread(() => OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2));
                        }else
                        if(Status == ClientStatus.Connecting){
                            // Check RSA server and send AES client
                            if(data.Value.Item4 == 0 && data.Value.Item2 == 0){
                                string PublicKeyRSA = Encoding.ASCII.GetString(data.Value.Item1);
                                if(PublicKeyRSA.StartsWith("<RSAKeyValue>") && PublicKeyRSA.EndsWith("</RSAKeyValue>") && PrivateKeyAES != null)
                                    dataServer.PublicKeyRSA = PublicKeyRSA;
                                return;
                            }
                            // Check AES server and connect
                            if(data.Value.Item4 == 0 && data.Value.Item2 == 1){
                                if(data.Value.Item1.Length == 16){
                                    dataServer.PrivateKeyAES = data.Value.Item1;
                                    dataServer.LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                    ChangeStatus(ClientStatus.Connected);
                                }
                                return;
                            }
                        }                        
                    });
                }
            }

            void Service(){
                while(Socket != null){
                    long Timer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    // Connect or reconnect client
                    if(Status == ClientStatus.Connecting && PublicKeyRSA != null && PrivateKeyAES != null)
                        if(connectingTimeoutTmp + ConnectingTimeout > Timer || ConnectingTimeout == 0){
                            if(dataServer.PublicKeyRSA == null)
                                SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA), 0, dataServer, background: true);
                            else
                                SendPacket(Socket, PrivateKeyAES, 1, dataServer, background: true); // groupID: 1 = AES
                        }else{
                            // Connection failed
                            ChangeStatus(ClientStatus.ConnectionFail);
                        }

                    // Check last timer connected and request ping value
                    if(Status == ClientStatus.Connected){
                        if(dataServer.LastTimer + ConnectTimeout < Timer){
                            ChangeStatus(ClientStatus.Connecting);
                        }
                        SendPing(Socket, [1]);

                        // Hold Connection
                        Parallel.ForEach(dataServer.ListHoldConnection.Values, bytes => {
                            SendPing(Socket, bytes);
                        });

                        // Queuing Hold Connection
                        if(dataServer.QueuingHoldConnection.Count > 0)
                            SendPing(Socket, dataServer.QueuingHoldConnection.ElementAt(0).Value);
                    }
                    Thread.Sleep(1000);
                }
            }

            void ChangeStatus(ClientStatus status){
                if(status != CurrentClientStatus){
                    connectingTimeoutTmp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    CurrentClientStatus = status;
                    
                    if(status == ClientStatus.IpBlocked || status == ClientStatus.MaxClientExceeded){
                        bool showLog = ShowLogDebug;
                        ShowLogDebug = false;
                        Disconnect();
                        ShowLogDebug = showLog;
                    }


                    if(ShowLogDebug)
                    switch(status){
                        case ClientStatus.Connecting:
                            ShowLog("[CLIENT] Connecting on " + IPEndPoint);
                        break;
                        case ClientStatus.Connected:
                            ShowLog("[CLIENT] Connected!");
                        break;
                        case ClientStatus.ConnectionFail:
                            ShowLog("[CLIENT] Unable to connect to the server.");
                        break;
                        case ClientStatus.Disconnecting:
                            ShowLog("[CLIENT] Disconnecting...");
                        break;
                        case ClientStatus.Disconnected:
                            ShowLog("[CLIENT] Disconnected!");
                        break;
                        case ClientStatus.IpBlocked:
                            ShowLog("[CLIENT] Your IP has been blocked by the server!");
                        break;
                        case ClientStatus.MaxClientExceeded:
                            ShowLog("[CLIENT] The maximum number of connected clients has been exceeded!");
                        break;
                    }
                    RunOnMainThread(() => OnStatus?.Invoke(status));
                }
            }

            /// <summary>
            /// Clear all events, data and free memory.
            /// </summary>
            public void Dispose(){
                // Is need clear events first to can clean ListRunMainThread in NethostfireService
                OnReceivedBytes = null;
                OnStatus = null;
                bool showLog = ShowLogDebug;
                ShowLogDebug = false;
                Disconnect();
                ShowLogDebug = showLog;
                GC.SuppressFinalize(this);
            }
        }
    }
}