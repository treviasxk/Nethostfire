// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.Utility;

namespace Nethostfire {
    public partial class UDP{
        public class Server : IDisposable{
            IPEndPoint? IPEndPoint;
            ConcurrentDictionary<IPEndPoint, DataClient> DataClients = new();
            ConcurrentDictionary<IPEndPoint, DataClient> QueuingClients = new();
            int connectedTimeout = 3000;
            bool showLogDebug = true;
            ServerStatus CurrentServerStatus = ServerStatus.Stopped;
            public int ConnectedTimeout {get{return connectedTimeout;} set{connectedTimeout = value;}}
            public bool ShowLogDebug {get{return showLogDebug;} set{showLogDebug = value;}}
            public Action<IPEndPoint>? OnConnected;
            public Action<IPEndPoint>? OnDisconnected;
            public Action<byte[], int, IPEndPoint>? OnReceivedBytes;
            public Action<ServerStatus>? OnStatus;
            public ServerStatus Status {get{return CurrentServerStatus;}} 
            public UdpClient? Socket;
            public ICollection<IPEndPoint> Clients {get{return DataClients.Keys;}}

            /// <summary>
            /// Start the server with specific IP, Port and sets the size of SymmetricSizeRSA if needed. If the server has already been started and then stopped you can call Server.Start(); without defining _host and _symmetricSizeRSA to start the server with the previous settings.
            /// </summary>
            public void Start(IPAddress ip, int port, int symmetricSizeRSA = 86){
                if(Socket == null){
                    StartUnity(server: this);
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

            /// <summary>
            /// The server will be stopped.
            /// </summary>
            public void Stop(){
                if(Socket != null){
                    ChangeStatus(ServerStatus.Stopping);
                    Socket.Close();
                    Socket = null;
                    DataClients.Clear();
                    QueuingClients.Clear();
                    ChangeStatus(ServerStatus.Stopped);
                }
            }


            /// <summary>
            /// All received PPS in a IP will be limited.
            /// </summary>
            public void ChangeLimitMaxPPS(int value, IPEndPoint ip){
                if(DataClients.TryGetValue(ip, out DataClient? dataClient))
                    dataClient.LimitMaxPPS = value;
            }

            /// <summary>
            /// All received PPS of groupID in a IP will be limited.
            /// </summary>
            public void ChangeLimitMaxPPS(int value, int groupID, IPEndPoint ip){
                if(DataClients.TryGetValue(ip, out DataClient? dataClient))
                    if(value == 0)
                        dataClient.LimitMaxPPSGroupID.TryRemove(groupID, out _);
                    else
                        if(dataClient.LimitMaxPPSGroupID.ContainsKey(groupID))
                            dataClient.LimitMaxPPSGroupID[groupID] = value;
                        else
                            dataClient.LimitMaxPPSGroupID.TryAdd(groupID, value);
            }


            /// <summary>
            /// To disconnect a client from server, it is necessary to inform the IP.
            /// </summary>
            public void Disconnect(IPEndPoint ip){
                if(DataClients.TryRemove(ip, out _)){
                    if(showLogDebug)
                        ShowLog(ip + " Disconnected!");
                    SendPing(Socket, [0], ip);
                }
            }

            /// <summary>
            /// To send bytes to a client, it is necessary to define the Bytes, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(byte[] bytes, int groupID, IPEndPoint ip) => SendPrepare(bytes, groupID, ip);
            /// <summary>
            /// To send bytes to a client, it is necessary to define the Bytes, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(byte[] bytes, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPrepare(bytes, groupID, ip, typeEncrypt, typeShipping);
            /// <summary>
            /// To send bytes to a client, it is necessary to define the Bytes, GroupID and IP, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void Send(byte[] bytes, int groupID, IPEndPoint ip, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendPrepare(bytes, groupID, ip, typeEncrypt, typeShipping);

            /// <summary>
            /// To send string to a client, it is necessary to define the Text, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(string text, int groupID, IPEndPoint ip) => SendPrepare(text, groupID, ip);
            /// <summary>
            /// To send string to a client, it is necessary to define the Text, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(string text, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPrepare(text, groupID, ip, typeEncrypt, typeShipping);
            /// <summary>
            /// To send string to a client, it is necessary to define the Text, GroupID and IP, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void Send(string text, int groupID, IPEndPoint ip, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendPrepare(text, groupID, ip, typeEncrypt, typeShipping);

            /// <summary>
            /// To send float to a client, it is necessary to define the Value, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(float value, int groupID, IPEndPoint ip) => SendPrepare(value, groupID, ip);
            /// <summary>
            /// To send float to a client, it is necessary to define the Value, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void Send(float value, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPrepare(value, groupID, ip, typeEncrypt, typeShipping);
            /// <summary>
            /// To send float to a client, it is necessary to define the Value, GroupID and IP, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void Send(float value, int groupID, IPEndPoint ip, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendPrepare(value, groupID, ip, typeEncrypt, typeShipping);


            /// <summary>
            /// To send bytes to a group client, it is necessary to define the Bytes, GroupID and IPs, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendGroup(byte[] bytes, int groupID, ConcurrentQueue<IPEndPoint> IPs) => SendGroupPrepare(bytes, groupID, IPs);
            /// <summary>
            /// To send bytes to a group client, it is necessary to define the Bytes, GroupID and IPs, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendGroup(byte[] bytes, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroupPrepare(bytes, groupID, IPs, typeEncrypt, typeShipping);
            /// <summary>
            /// To send bytes to a group client, it is necessary to define the Bytes, GroupID and IPs, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void SendGroup(byte[] bytes, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendGroupPrepare(bytes, groupID, IPs, typeEncrypt, typeShipping);

            /// <summary>
            /// To send string to a group client, it is necessary to define the Text, GroupID and IPs, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendGroup(string text, int groupID, ConcurrentQueue<IPEndPoint> IPs) => SendGroupPrepare(text, groupID, IPs);
            /// <summary>
            /// To send string to a group client, it is necessary to define the Text, GroupID and IPs, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendGroup(string text, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroupPrepare(text, groupID, IPs, typeEncrypt, typeShipping);
            /// <summary>
            /// To send string to a group client, it is necessary to define the Text, GroupID and IPs, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void SendGroup(string text, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendGroupPrepare(text, groupID, IPs, typeEncrypt, typeShipping);

            /// <summary>
            /// To send float to a group client, it is necessary to define the Value, GroupID and IPs, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendGroup(float value, int groupID, ConcurrentQueue<IPEndPoint> IPs) => SendGroupPrepare(value, groupID, IPs);
            /// <summary>
            /// To send float to a group client, it is necessary to define the Value, GroupID and IPs, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendGroup(float value, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroupPrepare(value, groupID, IPs, typeEncrypt, typeShipping);
            /// <summary>
            /// To send float to a group client, it is necessary to define the Value, GroupID and IPs, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void SendGroup(float value, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendGroupPrepare(value, groupID, IPs, typeEncrypt, typeShipping);


            /// <summary>
            /// To send bytes to all client, it is necessary to define the Bytes, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendAll(byte[] bytes, int groupID) => SendAllPrepare(bytes, groupID);
            /// <summary>
            /// To send bytes to all client, it is necessary to define the Bytes, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendAll(byte[] bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAllPrepare(bytes, groupID, typeEncrypt, typeShipping);
            /// <summary>
            /// To send bytes to all client, it is necessary to define the Bytes, GroupID and IP, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void SendAll(byte[] bytes, int groupID, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendAllPrepare(bytes, groupID, typeEncrypt, typeShipping);

            /// <summary>
            /// To send string to all client, it is necessary to define the Text, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendAll(string text, int groupID) => SendAllPrepare(text, groupID);
            /// <summary>
            /// To send string to all client, it is necessary to define the Text, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendAll(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAllPrepare(text, groupID, typeEncrypt, typeShipping);
            /// <summary>
            /// To send string to all client, it is necessary to define the Text, GroupID and IP, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void SendAll(string text, int groupID, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendAllPrepare(text, groupID, typeEncrypt, typeShipping);

            /// <summary>
            /// To send float to all client, it is necessary to define the Value, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendAll(float value, int groupID) => SendAllPrepare(value, groupID);
            /// <summary>
            /// To send float to all client, it is necessary to define the Value, GroupID and IP, the other sending resources such as TypeEncrypt and TypeShipping are optional.
            /// </summary>
            public void SendAll(float value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAllPrepare(value, groupID, typeEncrypt, typeShipping);
            /// <summary>
            /// To send float to all client, it is necessary to define the Value, GroupID and IP, the other sending resources such as TypeShipping and TypeEncrypt are optional.
            /// </summary>
            public void SendAll(float value, int groupID, TypeShipping typeShipping = TypeShipping.None, TypeEncrypt typeEncrypt = TypeEncrypt.None) => SendAllPrepare(value, groupID, typeEncrypt, typeShipping);

            // Send to one IP
            void SendPrepare(byte[] bytes, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && bytes != null)
                    SendPacket(Socket, bytes, groupID, DataClients[ip], typeEncrypt, typeShipping, ip);
            }

            void SendPrepare(string text, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && text != null)
                    SendPacket(Socket, Encoding.UTF8.GetBytes(text), groupID, DataClients[ip], typeEncrypt, typeShipping, ip);
            }

            void SendPrepare(float value, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running)
                    SendPacket(Socket, BitConverter.GetBytes(value), groupID, DataClients[ip], typeEncrypt, typeShipping, ip);
            }

            // Send to group IPs
            void SendGroupPrepare(byte[] bytes, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && bytes != null)
                    Parallel.ForEach(IPs, ip => SendPrepare(bytes, groupID, ip, typeEncrypt, typeShipping));
            }

            void SendGroupPrepare(string text, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && text != null)
                    Parallel.ForEach(IPs, ip => SendPrepare(Encoding.UTF8.GetBytes(text), groupID, ip, typeEncrypt, typeShipping));
            }

            void SendGroupPrepare(float value, int groupID, ConcurrentQueue<IPEndPoint> IPs, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running)
                    Parallel.ForEach(IPs, ip => SendPrepare(BitConverter.GetBytes(value), groupID, ip, typeEncrypt, typeShipping));
            }

            // Send to all IPs
            void SendAllPrepare(byte[] bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && bytes != null)
                    Parallel.ForEach(DataClients.Keys, ip => SendPrepare(bytes, groupID, ip, typeEncrypt, typeShipping));
            }

            void SendAllPrepare(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && text != null)
                    Parallel.ForEach(DataClients.Keys, ip => SendPrepare(Encoding.UTF8.GetBytes(text), groupID, ip, typeEncrypt, typeShipping));
            }

            void SendAllPrepare(float value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running)
                    Parallel.ForEach(DataClients.Keys, ip => SendPrepare(BitConverter.GetBytes(value), groupID, ip, typeEncrypt, typeShipping));
            }


            async void ReceivePackage(){
                while(Socket != null){
                    // Receive bytes and ip
                    var receivedResult = await Socket.ReceiveAsync();
                    byte[] bytes  = receivedResult.Buffer;
                    IPEndPoint ip = receivedResult.RemoteEndPoint;

                    Parallel.Invoke(()=>{
                        // Connected
                        if(DataClients.TryGetValue(ip, out var dataClient)){
                            // Commands
                            if(bytes.Length == 1){
                                switch(bytes[0]){
                                    case 0: // Force disconnect
                                        Disconnect(ip);
                                    return;
                                    case 1: // Confirm Online
                                        DataClients[ip].LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                        SendPing(Socket, [1], ip);
                                    return;
                                }
                            }
                            // item1 = bytes
                            // item2 = groupID
                            // item3 = typeEncrypt
                            // item4 = typeShipping
                            var data = BytesToReceive(Socket, bytes, dataClient, ip);
                            if(data.HasValue && data.Value.Item4 != 0)
                                RunOnMainThread(() => OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2, ip));
                            return;
                        }else{
                            if(QueuingClients.TryGetValue(ip, out var _queuingClients))
                                dataClient = _queuingClients;
                            else
                                dataClient = new DataClient();


                            // item1 = bytes
                            // item2 = groupID
                            // item3 = typeEncrypt
                            // item4 = typeShipping
                            var data = BytesToReceive(Socket, bytes, dataClient, ip);        
                            // Check RSA client received and send RSA server
                            if(data.HasValue && data.Value.Item4 == 0 && data.Value.Item2 == 0 && PublicKeyRSA != null){
                                string PublicKeyRSAClient = Encoding.ASCII.GetString(data.Value.Item1);
                                if(PublicKeyRSAClient.StartsWith("<RSAKeyValue>") && PublicKeyRSAClient.EndsWith("</RSAKeyValue>")){
                                    dataClient.PublicKeyRSA = PublicKeyRSAClient;
                                    QueuingClients.TryAdd(ip, dataClient);
                                    SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA), 0, dataClient, ip: ip, background: true);  // groupID: 0 = RSA
                                }
                                return;
                            }
                            // Check AES client, send AES server and connect client
                            if(data.HasValue && data.Value.Item4 == 0 && data.Value.Item2 == 1 && PrivateKeyAES != null){
                                dataClient.PrivateKeyAES = data.Value.Item1;
                                dataClient.MaxPPSTimer = 0;
                                dataClient.LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                if(QueuingClients.TryRemove(ip, out _)){
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

            void Service(){
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

            void ChangeStatus(ServerStatus status){
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
                Stop();
                ShowLogDebug = showLog;
                GC.SuppressFinalize(this);
            }
        }
    }
}