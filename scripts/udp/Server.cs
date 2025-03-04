// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.System;

namespace Nethostfire {
    public partial class UDP{
        public class Server : IDisposable{
            public UdpClient? Socket;
            IPEndPoint? IPEndPoint;
            ConcurrentDictionary<IPAddress, long> ipBlockeds = new();
            ConcurrentDictionary<IPEndPoint, DataClient> DataClients = new();
            ConcurrentDictionary<IPEndPoint, DataClient> QueuingClients = new();
            ServerStatus CurrentServerStatus = ServerStatus.Stopped;

            /// <summary>
            /// ConnectTimeout is the maximum time limit in milliseconds that a client can remain connected to the server when a ping is not received. (default value is 3000)
            /// </summary>
            public int ConnectedTimeout {get; set;} = 3000;

            /// <summary>
            /// OnConnected is an event that you can use to receive the IP whenever a client connected.
            /// </summary>
            public Action<IPEndPoint>? OnConnected;

            /// <summary>
            /// OnDisconnected is an event that you can use to receive the IP whenever a client disconnected.
            /// </summary>
            public Action<IPEndPoint>? OnDisconnected;

            /// <summary>
            /// OnReceivedBytes an event that returns bytes received, GroupID and IP whenever the received bytes by clients, with it you can manipulate the bytes received.
            /// </summary>
            public Action<byte[], int, IPEndPoint>? OnReceivedBytes;

            /// <summary>
            /// OnStatus is an event that returns ServerStatus whenever the status changes, with which you can use it to know the current status of the server.
            /// </summary>
            public Action<ServerStatus>? OnStatus;

            /// <summary>
            /// The Status is an enum ServerStatus with it you can know the current state of the server.
            /// </summary>
            public ServerStatus Status {get{return CurrentServerStatus;}} 

            /// <summary>
            /// The DebugLog when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
            /// </summary>
            public bool DebugLog {get; set;} = true;

            /// <summary>
            /// MaxClients is the maximum number of clients that can connect to the server. If you have many connected clients and you change the value below the number of connected clients, they will not be disconnected, the server will block new connections until the number of connected clients is below or equal to the limit. (The default value is 0, which is unlimited clients)
            /// </summary>
            public int MaxClients {get; set;} = 0;

            /// <summary>
            /// The ClientsCount is all IPs of clients connected to the server.
            /// </summary>
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
            /// Blocks a specific IP for the time defined in milliseconds. If the time is 0, the IP will be blocked until the server is restarted.
            /// </summary>
            public void BlockIP(IPAddress ip, int timer = 0) => ipBlockeds.AddOrUpdate(ip, timer == 0 ? 0 : (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + timer, (ip, timer) => timer == 0 ? 0 : (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + timer);

            /// <summary>
            /// Unblock a specific IP if it has been blocked with BlockIP.
            /// </summary>
            public void UnlockIP(IPAddress ip) => ipBlockeds.TryRemove(ip, out _);

            /// <summary>
            /// All received PPS in a IP will be limited.
            /// </summary>
            public void ChangeLimitMaxPPS(int pps, IPEndPoint ip){
                if(DataClients.TryGetValue(ip, out DataClient dataClient))
                    dataClient.LimitMaxPPS = pps;
            }

            /// <summary>
            /// All received PPS of groupID in a IP will be limited.
            /// </summary>
            public void ChangeLimitMaxPPS(int pps, int groupID, IPEndPoint ip){
                if(DataClients.TryGetValue(ip, out DataClient dataClient))
                    if(pps == 0)
                        dataClient.LimitMaxPPSGroupID.TryRemove(groupID, out _);
                    else
                        dataClient.LimitMaxPPSGroupID.AddOrUpdate(groupID, pps, (groupID, pps) => pps);
            }

            /// <summary>
            /// To kick a client connected from server, it is necessary to inform the IP.
            /// </summary>
            public void Kick(IPEndPoint ip){
                // kick is Only for client connected.
                if(DataClients.ContainsKey(ip)){
                    OnDisconnected?.Invoke(ip);
                    if(DataClients.TryRemove(ip, out _)){
                        ShowLog(ip + " Kicked!");
                        SendPing(Socket, [0], ip);
                    }
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
                if(Status == ServerStatus.Running && bytes != null && DataClients.ContainsKey(ip))
                    SendPacket(Socket, bytes, groupID, DataClients[ip], typeEncrypt, typeShipping, ip);
            }

            void SendPrepare(string text, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && text != null && DataClients.ContainsKey(ip))
                    SendPacket(Socket, Encoding.UTF8.GetBytes(text), groupID, DataClients[ip], typeEncrypt, typeShipping, ip);
            }

            void SendPrepare(float value, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Status == ServerStatus.Running && DataClients.ContainsKey(ip))
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
                    byte[] bytes;
                    IPEndPoint ip;
                    // Connection alway fail when ip not found, use 'try' is necessary.
                    try{
                        // Receive bytes and ip
                        var receivedResult = await Socket.ReceiveAsync();
                        bytes = receivedResult.Buffer;
                        ip = receivedResult.RemoteEndPoint;
                    }catch{
                        continue;
                    }

                    // Check IP blocked.
                    if(ipBlockeds.TryGetValue(ip.Address, out var timer)){
                        if(timer == 0 || timer > DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond){
                            QueuingClients.TryRemove(ip, out _);
                            SendPing(Socket, [3], ip);
                            return;
                        }else
                            ipBlockeds.TryRemove(ip.Address, out _);
                    }

                    if(bytes != null && ip != null)
                    Parallel.Invoke(()=>{
                        (byte[], int, TypeEncrypt, int)? data;
                        
                        // Connected
                        if(DataClients.TryGetValue(ip, out var dataClient)){
                            // Update time online
                            dataClient.LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                            DataClients[ip] = dataClient;
                            // Commands
                            if(bytes.Length == 1){
                                switch(bytes[0]){
                                    case 0: // Force disconnect
                                        Kick(ip);
                                    return;
                                    case 1: // Update ping
                                        SendPing(Socket, [1], ip);
                                    return;
                                }
                            }

                            // item1 = bytes
                            // item2 = groupID
                            // item3 = typeEncrypt
                            // item4 = typeShipping
                            data = BytesToReceive(Socket, bytes, dataClient, ip);
                            if(data.HasValue && data.Value.Item4 != 0){
                                RunOnMainThread(() => OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2, ip));
                                return;
                            }
                        }else{
                            // clients max execedeed
                            if(MaxClients != 0 && Clients.Count >= MaxClients){
                                SendPing(Socket, [2], ip);
                                return;
                            }

                            // Update LastTimer
                            dataClient.LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                            // Check if Client in Queuing
                            if(!QueuingClients.TryGetValue(ip, out dataClient))
                                dataClient = new(){LastTimer = dataClient.LastTimer, ListIndex = new(), ListHoldConnection = new(), QueuingHoldConnection = new(), LimitMaxPPSGroupID = new()};
                            else{
                                dataClient.LastTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                QueuingClients[ip] = dataClient;
                            }
 
                            // item1 = bytes
                            // item2 = groupID
                            // item3 = typeEncrypt
                            // item4 = typeShipping
                            data = BytesToReceive(Socket, bytes, dataClient, ip);
                        }

                        
                        // Check RSA client received and send RSA server
                        if(data.HasValue && data.Value.Item4 == 0 && data.Value.Item2 == 0 && PublicKeyRSA != null){                         
                            string PublicKeyRSAClient = Encoding.ASCII.GetString(data.Value.Item1);
                            if(PublicKeyRSAClient.StartsWith("<RSAKeyValue>") && PublicKeyRSAClient.EndsWith("</RSAKeyValue>")){
                                dataClient.PublicKeyRSA = PublicKeyRSAClient;
                                if(QueuingClients.TryAdd(ip, dataClient))
                                    ShowLog(ip + " Incoming...");

                                // Send PublicKeyRSA
                                SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA), 0, dataClient, ip: ip, background: true);  // groupID: 0 = RSA
                            }
                            return;
                        }


                        // Check AES client, send AES server and connect client
                        if(data.HasValue && data.Value.Item4 == 0 && data.Value.Item2 == 1 && PrivateKeyAES != null){
                            dataClient.PrivateKeyAES = data.Value.Item1;
                            dataClient.MaxPPSTimer = 0;
                            if(QueuingClients.TryRemove(ip, out _) && DataClients.TryAdd(ip, dataClient)){
                                OnConnected?.Invoke(ip);
                                ShowLog(ip + " Connected!");
                            }

                            // Send PrivateKeyAES
                            SendPacket(Socket, PrivateKeyAES, 1, dataClient, ip: ip, background: true);  // groupID: 1 = AES
                            return;
                        }
                        
                    });
                }
            }

            void Service(){
                while(Socket != null){
                    // Check timer connection dataClients.
                    Parallel.ForEach(DataClients.Where(item => item.Value.LastTimer + ConnectedTimeout < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond), item =>{
                        OnDisconnected?.Invoke(item.Key);
                        if(DataClients.TryRemove(item.Key, out _))
                            ShowLog(item.Key + " Disconnected!");
                    });

                    // Check timer connection queuingClients.
                    Parallel.ForEach(QueuingClients.Where(item => item.Value.LastTimer + ConnectedTimeout < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond), item =>{    // Problema no LastTimer
                        if(QueuingClients.TryRemove(item.Key, out _))
                            ShowLog(item.Key + " Connection lost!");
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
                    switch(status){
                        case ServerStatus.Initializing:
                            ShowLog("Initializing...");
                        break;
                        case ServerStatus.Running:
                            ShowLog("Initialized and hosted on " + IPEndPoint);
                        break;
                        case ServerStatus.Restarting:
                            ShowLog("Restarting...");
                        break;
                        case ServerStatus.Stopping:
                            ShowLog("Stopping...");
                        break;
                        case ServerStatus.Stopped:
                            ShowLog("Stopped.");
                        break;
                    }
                    RunOnMainThread(() => OnStatus?.Invoke(status));
                }
            }

            /// <summary>
            /// Create a server log, if SaveLog is enabled, the message will be saved in the logs.
            /// </summary>
            public void ShowLog(string message){
                if(DebugLog)
                    Log("[SERVER] " + message, SaveLog);
            }

            /// <summary>
            /// Clear all events, data and free memory.
            /// </summary>
            public void Dispose(){
                // Is need clear events first to can clean ListRunMainThread in NethostfireService
                OnReceivedBytes = null;
                OnStatus = null;
                bool showLog = DebugLog;
                DebugLog = false;
                Stop();
                DebugLog = showLog;
                GC.SuppressFinalize(this);
            }
        }
    }
}