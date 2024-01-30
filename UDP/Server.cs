using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.Utility;

namespace Nethostfire {
    public partial class UDP{
        public class Server{
            IPEndPoint? IPEndPoint;
            ConcurrentDictionary<IPEndPoint, DataClient> DataClients = new();
            ConcurrentDictionary<IPEndPoint, DataClient> QueuingClients = new();
            int connectedTimeout = 3000;
            bool showLogDebug = true, showUnityNetworkStatistics;
            ServerStatus CurrentServerStatus = ServerStatus.Stopped;
            public int ConnectedTimeout {get{return connectedTimeout;} set{connectedTimeout = value;}}
            public bool ShowLogDebug {get{return showLogDebug;} set{showLogDebug = value;}}
            public bool ShowUnityNetworkStatistics {get{return showUnityNetworkStatistics;} set{showUnityNetworkStatistics = value;}}
            public Action<IPEndPoint>? OnConnected;
            public Action<IPEndPoint>? OnDisconnected;
            public Action<byte[], int, IPEndPoint>? OnReceivedBytes;
            public Action<ServerStatus>? OnStatus;
            public ServerStatus Status {get{return CurrentServerStatus;}} 
            public UdpClient? Socket;
            public ICollection<IPEndPoint> Clients {get{return DataClients.Keys;}}
            public void Start(IPAddress ip, int port, int symmetricSizeRSA = 86){
                if(Socket == null){
                    ChangeStatus(ServerStatus.Initializing);
                    Socket = new UdpClient();
                    GenerateKey(symmetricSizeRSA);
                    try{
                        IPEndPoint = new IPEndPoint(ip, port);
                        Socket.Client.Bind(IPEndPoint);
                        new Thread(ReceivePackage){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                        new Thread(Service){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                        ChangeStatus(ServerStatus.Running);
                    }catch{
                        Stop();
                        throw new Nethostfire("Could not start the server, check that the port "+ IPEndPoint?.Port + " is not blocked, or that you have other software using that port.");
                    }
                }
            }

            private void Service(){
                while(Socket != null){
                    // Check timer connection.
                    Parallel.ForEach(DataClients.Where(item => item.Value.LastTimer + connectedTimeout < (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)), item =>{
                        if(DataClients.TryRemove(item.Key, out _))
                            if(showLogDebug)
                                ShowLog(item.Key + " Disconnected!");
                    });

                    // Hold Connection
                    Parallel.ForEach(DataClients.Where(item => item.Value.ListHoldConnection.Count > 0), item => {
                        Parallel.ForEach(item.Value.ListHoldConnection.Values, bytes => {
                            SendPing(Socket, bytes, item.Key);
                        });
                    });

                    // Queuing Hold Connection
                    Parallel.ForEach(DataClients.Where(item => item.Value.QueuingHoldConnection.Count > 0), item => {
                        SendPing(Socket, item.Value.QueuingHoldConnection.ElementAt(0).Value, item.Key);
                    });
                    Thread.Sleep(1000);
                }
            }

            public void Stop(){
                if(Socket != null){
                    Socket.Close();
                    Socket = null;
                    DataClients.Clear();
                    QueuingClients.Clear();
                    ChangeStatus(ServerStatus.Stopped);
                }
            }

            public void Send(byte[] bytes, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running)
                    SendPacket(Socket, bytes, groupID, DataClients[ip], typeEncrypt, typeShipping, ip);
            }

            async void ReceivePackage(){
                while(Socket != null){
                    // Receive bytes and ip
                    var receivedResult = await Socket.ReceiveAsync();
                    byte[] bytes  = receivedResult.Buffer;
                    IPEndPoint ip = receivedResult.RemoteEndPoint;

                    Parallel.Invoke(()=>{
                        // item1 = bytes
                        // item2 = groupID
                        // item3 = typeEncrypt
                        // item4 = typeShipping
                        var data = BytesToReceive(Socket, bytes, DataClients.ContainsKey(ip) ? DataClients[ip] : null, DataClients.ContainsKey(ip), ip);
                        if(DataClients.ContainsKey(ip)){
                            // Commands
                            if(bytes.Length == 1){
                                // Confirm Online
                                if(bytes[0] == 1){
                                    DataClients[ip].LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                    SendPing(Socket, [1], ip);
                                }
                                return;
                            }
                            // Connected
                            if(data.HasValue)
                                if(data.Value.Item4 != 0)
                                    OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2, ip);
                            return;
                            
                        }else
                        // Connecting client
                        if(data.HasValue){
                            // Check RSA client received and send RSA server
                            if(data.Value.Item4 == 0 && data.Value.Item2 == 0){
                                string PublicKeyRSAClient = Encoding.ASCII.GetString(data.Value.Item1);
                                if(PublicKeyRSAClient.StartsWith("<RSAKeyValue>") && PublicKeyRSAClient.EndsWith("</RSAKeyValue>") && PublicKeyRSA != null){
                                    DataClient dataClient = new DataClient(){PublicKeyRSA = PublicKeyRSAClient};
                                    QueuingClients.TryAdd(ip, dataClient);
                                    SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA), 0, dataClient, ip: ip, background: true);  // groupID: 0 = RSA
                                }
                                return;
                            }
                            // Check AES client, send AES server and connect client
                            if(data.Value.Item4 == 0 && data.Value.Item2 == 1 && QueuingClients.ContainsKey(ip) && PrivateKeyAES != null){
                                QueuingClients[ip].PrivateKeyAES = data.Value.Item1;
                                QueuingClients[ip].LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                if(QueuingClients.TryRemove(ip, out DataClient? dataClient)){
                                    DataClients.TryAdd(ip, dataClient);
                                    if(showLogDebug)
                                        ShowLog(ip + " connected to the server.");
                                    OnConnected?.Invoke(ip);
                                    SendPacket(Socket, PrivateKeyAES, 1, dataClient, ip: ip, background: true);  // groupID: 1 = AES
                                }
                                return;
                            }
                        }
                    });
                }
            }

            void ChangeStatus(ServerStatus status){
                StartUnity(server: this);
                if(status != CurrentServerStatus){
                    CurrentServerStatus = status;

                    if(showLogDebug)
                    switch(status){
                        case ServerStatus.Initializing:
                            ShowLog("Initializing server...");
                        break;
                        case ServerStatus.Running:
                            ShowLog("Server initialized and hosted on " + IPEndPoint);
                        break;
                        case ServerStatus.Restarting:
                            ShowLog("Restarting server...");
                        break;
                        case ServerStatus.Stopping:
                            ShowLog("Stopping Server...");
                        break;
                        case ServerStatus.Stopped:
                            ShowLog("Server stopped.");
                        break;
                    }
                    OnStatus?.Invoke(status);
                }
            }
        }
    }
}