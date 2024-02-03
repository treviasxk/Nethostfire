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
            IPEndPoint? IPEndPoint;
            DataClient dataServer = new();
            int connectTimeout = 3000, connectingTimeout = 10000;
            bool showLogDebug = true;
            ClientStatus CurrentClientStatus = ClientStatus.Disconnected;
            long connectingTimeoutTmp;
            public int ConnectTimeout {get{return connectTimeout;} set{connectTimeout = value;}}
            public int ConnectingTimeout {get{return connectingTimeout;} set{connectingTimeout = value;}}
            public bool ShowLogDebug {get{return showLogDebug;} set{showLogDebug = value;}}
            public int Ping {get {return dataServer.Ping;}}
            public Action<ClientStatus>? OnStatus;
            public Action<byte[], int>? OnReceivedBytes;
            public ClientStatus Status {get{return CurrentClientStatus;}} 
            public UdpClient? Socket;

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

            private void Service(){
                while(Socket != null){
                    // Connect or reconnect client
                    if(Status == ClientStatus.Connecting && PublicKeyRSA != null && PrivateKeyAES != null)
                        if(connectingTimeoutTmp + connectingTimeout > (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) || connectingTimeout == 0){
                            SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA), 0, dataServer, background: true);
                        }else{
                            // Connection failed
                            ChangeStatus(ClientStatus.ConnectionFail);
                        }

                    // Check last timer connected and request ping value
                    if(Status == ClientStatus.Connected){
                        if(dataServer.LastTimer + connectTimeout < (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)){
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

                    // Connection client alway fail when ip with server not found, use 'try' is necessary.
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
                            long Timer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                            dataServer.Ping = Convert.ToInt32(Timer - dataServer.LastTimer);
                            dataServer.LastTimer = Timer;
                            return;
                        }

                        // item1 = bytes
                        // item2 = groupID
                        // item3 = typeEncrypt
                        // item4 = typeShipping
                        var data = BytesToReceive(Socket, bytes, dataServer, Status == ClientStatus.Connected);
                        if(data.HasValue)
                        if(Status == ClientStatus.Connected){
                            RunOnMainThread(() => OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2));
                        }else
                        if(Status == ClientStatus.Connecting){
                            // Check RSA server and send AES client
                            if(data.Value.Item4 == 0 && data.Value.Item2 == 0){
                                string PublicKeyRSA = Encoding.ASCII.GetString(data.Value.Item1);
                                if(PublicKeyRSA.StartsWith("<RSAKeyValue>") && PublicKeyRSA.EndsWith("</RSAKeyValue>") && PrivateKeyAES != null){
                                    dataServer.PublicKeyRSA = PublicKeyRSA;
                                    SendPacket(Socket, PrivateKeyAES, 1, dataServer, background: true); // groupID: 1 = AES
                                }
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

            void ChangeStatus(ClientStatus status){
                if(status != CurrentClientStatus){
                    connectingTimeoutTmp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    CurrentClientStatus = status;

                    if(showLogDebug)
                    switch(status){
                        case ClientStatus.Connecting:
                            ShowLog("Connecting on " + IPEndPoint);
                        break;
                        case ClientStatus.Connected:
                            ShowLog("Connected!");
                        break;
                        case ClientStatus.ConnectionFail:
                            ShowLog("Unable to connect to the server.");
                        break;
                        case ClientStatus.Disconnecting:
                            ShowLog("Disconnecting...");
                        break;
                        case ClientStatus.Disconnected:
                            ShowLog("Disconnected!");
                        break;
                        case ClientStatus.IpBlocked:
                            ShowLog("Your IP has been blocked by the server!");
                        break;
                        case ClientStatus.MaxClientExceeded:
                            ShowLog("The maximum number of connected clients has been exceeded!");
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