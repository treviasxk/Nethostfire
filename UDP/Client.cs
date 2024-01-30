using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.Utility;

namespace Nethostfire {
    public partial class UDP{
        public class Client{
            int connectTimeout = 3000, connectingTimeout = 10000;
            ClientStatus CurrentClientStatus = ClientStatus.Disconnected;
            long connectingTimeoutTmp;
            public int ConnectTimeout {get{return connectTimeout;} set{connectTimeout = value;}}
            public int ConnectingTimeout {get{return connectingTimeout;} set{connectingTimeout = value;}}
            public int Ping {get {return dataServer.Ping;}}
            public Action<ClientStatus>? OnStatus;
            public Action<byte[], int>? OnReceivedBytes;
            public ClientStatus Status {get{return CurrentClientStatus;}} 
            public UdpClient? Socket;
            IPEndPoint? IPEndPoint;
            DataClient dataServer = new();
            public void Connect(IPAddress ip, int port, int symmetricSizeRSA = 86){
                if(Socket == null){
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

            public void Disconnect(){
                if(Socket != null){
                    ChangeStatus(ClientStatus.Disconnecting);
                    Socket.Close();
                    Socket = null;
                    dataServer = new();
                    ChangeStatus(ClientStatus.Disconnected);
                }
            }

            public void Send(byte[] bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ClientStatus.Connected)
                    SendPacket(Socket, bytes, groupID, dataServer, typeEncrypt, typeShipping);
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
                            OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2);
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
                    OnStatus?.Invoke(status);
                }
            }

        }
    }
}