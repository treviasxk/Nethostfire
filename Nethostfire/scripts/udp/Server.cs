// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.System;
using static Nethostfire.DataSecurity;
using System.Collections.Concurrent;

namespace Nethostfire {
    public partial class UDP{
        public class Server : IDisposable{
            public UdpClient? Socket;
            public int LimitPPS {get;set;} = 0;
            ServerStatus serverStatus = ServerStatus.Stopped;
            ConcurrentDictionary<int, int> ListReceiveGroudIdPPS = new();
            ConcurrentDictionary<int, int> ListSendGroudIdPPS = new();
            /// <summary>
            /// ConnectTimeout is the maximum time limit in milliseconds that a client can remain connected to the server when a ping is not received. (default value is 3000)
            /// </summary>
            public int ConnectedTimeout {get; set;} = 3000;

            /// <summary>
            /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
            /// </summary>
            public bool EnableLogs {get; set;} = true;

            /// <summary>
            /// The ClientsCount is all IPs of clients connected to the server.
            /// </summary>
            public ICollection<IPEndPoint> Clients {get{return Sessions.Keys;}}

            /// <summary>
            /// OnStatus is an event that returns ServerStatus whenever the status changes, with which you can use it to know the current status of the server.
            /// </summary>
            public Action<ServerStatus>? OnStatus;
            
            /// <summary>
            /// OnReceivedBytes an event that returns bytes received, GroupID and IP whenever the received bytes by clients, with it you can manipulate the bytes received.
            /// </summary>
            public Action<byte[], int, IPEndPoint>? OnReceivedBytes;
            /// <summary>
            /// OnConnected is an event that you can use to receive the IP whenever a client connected.
            /// </summary>
            public Action<IPEndPoint>? OnConnected;

            /// <summary>
            /// OnDisconnected is an event that you can use to receive the IP whenever a client disconnected.
            /// </summary>
            public Action<IPEndPoint>? OnDisconnected;

            public ServerStatus Status {get {return serverStatus;}}

            public ConcurrentDictionary<IPEndPoint, Session> Sessions = new();

            public void Start(IPAddress Host, Int16 Port = 0, int symmetricSizeRSA = 86){
                try{
                    if(Socket == null){
                        Socket ??= new UdpClient();
                        StartUnity(server: this);
                        GenerateKey(symmetricSizeRSA);
                        ChangeStatus(ServerStatus.Initializing);
                        Socket.Client.Bind(new IPEndPoint(Host, Port));
                        new Thread(Service){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                        
                        new Thread(ReceivePacket){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                        WriteLog($"Hosted in: {Host}:{Port}", this, EnableLogs);
                        ChangeStatus(ServerStatus.Running, false);
                    }
                }catch(Exception ex){
                    throw new Nethostfire(ex.Message, this);
                }
            }

            public void SetReceiveLimitGroupPPS(ushort groupID, int pps){
                if(pps > 0)
                    ListReceiveGroudIdPPS.TryAdd(groupID, pps);
                else
                    ListReceiveGroudIdPPS.TryRemove(groupID, out _);
            }

            public void SetSendLimitGroupPPS(ushort groupID, int pps){
                if(pps > 0)
                    ListSendGroudIdPPS.TryAdd(groupID, pps);
                else
                    ListSendGroudIdPPS.TryRemove(groupID, out _);
            }

            public void Send(byte[]? bytes, int groupID, ref IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
                if(Sessions.TryGetValue(ip, out var session))
                    SendPacket(Socket, ref bytes, groupID, typeEncrypt, typeShipping, ref session, ip, in ListSendGroudIdPPS);
            }
            
            public void Send(string text, int groupID, ref IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(Encoding.UTF8.GetBytes(text), groupID, ref ip, typeEncrypt, typeShipping);
            public void Send(int value, int groupID, ref IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(BitConverter.GetBytes(value), groupID, ref ip, typeEncrypt, typeShipping);
            public void Send(object data, int groupID, ref IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(Json.GetBytes(data), groupID, ref ip, typeEncrypt, typeShipping);

            public void SendGroup(byte[]? bytes, int groupID, ref ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Parallel.ForEach(ips, (ip) => Send(bytes, groupID, ref ip, typeEncrypt, typeShipping));
            public void SendGroup(string text, int groupID, ref ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroup(Encoding.UTF8.GetBytes(text), groupID, ref ips, typeEncrypt, typeShipping);
            public void SendGroup(int value, int groupID, ref ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroup(BitConverter.GetBytes(value), groupID, ref ips, typeEncrypt, typeShipping);
            public void SendGroup(object data, int groupID, ref ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroup(Json.GetBytes(data), groupID, ref ips, typeEncrypt, typeShipping);
            
            public void SendAll(byte[]? bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Parallel.ForEach(Sessions.Keys, (ip) => Send(bytes, groupID, ref ip, typeEncrypt, typeShipping));
            public void SendAll(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAll(Encoding.UTF8.GetBytes(text), groupID, typeEncrypt, typeShipping);
            public void SendAll(int value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAll(BitConverter.GetBytes(value), groupID, typeEncrypt, typeShipping);
            public void SendAll(object data, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAll(Json.GetBytes(data), groupID, typeEncrypt, typeShipping);

            async void ReceivePacket(){
                while(Socket != null){
                    byte[]? bytes;
                    IPEndPoint? ip;
                    
                    // Connection alway fail when ip not found, use 'try' is necessary.
                    try{
                        // Receive bytes and ip
                        var receivedResult = await Socket.ReceiveAsync();
                        bytes = receivedResult.Buffer;
                        ip = receivedResult.RemoteEndPoint;
                    }catch{
                        continue;
                    }


                    if(bytes != null && ip != null)
                    Parallel.Invoke(()=>{
                        var Authenticated = Sessions.TryGetValue(ip, out Session session);
                        var data = DeconvertPacket(Socket, bytes, ref session, ip);

                        if(data.HasValue)
                        if(Authenticated){
                            // Commands
                            if(bytes.Length == 1){
                                switch(bytes[0]){
                                    case 0: // Force disconnect
                                        //Kick(ip);
                                    return;
                                    case 1: // Update ping
                                        session.Ping = GetPing(session.Timer);
                                        session.Timer = DateTime.Now.Ticks;
                                        SendPing(Socket, [1], ip);
                                    return;
                                    case 2:
                                        SendPing(Socket, [1], ip);
                                    return;
                                }
                            }else{
                                if(session.PublicKeyRSA == "" || session.PrivateKeyAES == null){
                                    switch(data.Value.Item2){
                                        case 0:
                                            // Resend RSA
                                            var bytes = Encoding.ASCII.GetBytes(PublicKeyRSA!);
                                            SendPacket(Socket, ref bytes, 0, TypeEncrypt.None, TypeShipping.None, ref session, ip);
                                        return;
                                        case 1:
                                            // AES
                                            session.PrivateKeyAES = data.Value.Item1;
                                            var key = PrivateKeyAES;
                                            SendPacket(Socket, ref key, 1, TypeEncrypt.RSA, TypeShipping.None, ref session, ip);
                                            if(session.Status == SessionStatus.Connecting){
                                                WriteLog($"{ip} Connected!", this, EnableLogs);
                                                session.Timer = DateTime.Now.Ticks;
                                                session.Status = SessionStatus.Connected;
                                                RunOnMainThread(() => OnConnected?.Invoke(ip));
                                            }
                                        return;
                                    }
                                }else{
                                    if(CheckReceiveLimitPPS(data.Value.Item1, data.Value.Item2, ref session, LimitPPS, in ListReceiveGroudIdPPS)){
                                        RunOnMainThread(() => OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2, ip));
                                    }
                                    return;
                                }
                            }
                        }else{
                            // Check Authenticated
                            if(data != null && data.Value.Item2 == 0){  // groupID = 0
                                string value = Encoding.ASCII.GetString(data.Value.Item1);
                                if(value.StartsWith("<RSAKeyValue>") && value.EndsWith("</RSAKeyValue>")){
                                    session = new();
                                    session.PublicKeyRSA = value;
                                    session.Timer = DateTime.Now.Ticks;
                                    session.Status = SessionStatus.Connecting;
                                    if(Sessions.TryAdd(ip, session)){
                                        WriteLog($"{ip} Incomming...", this, EnableLogs);
                                        var bytes = Encoding.ASCII.GetBytes(PublicKeyRSA!);
                                        SendPacket(Socket, ref bytes, 0, TypeEncrypt.Compress, TypeShipping.None, ref session, ip);
                                    }
                                }
                            }
                        }
                    });
                }
            }

            public void Stop(){
                ChangeStatus(ServerStatus.Stopping);
                Dispose();
                ChangeStatus(ServerStatus.Stopped);
            }

            void Service(){
                while(Socket != null){
                    // Check timer connection dataClients.
                    Parallel.ForEach(Sessions.Where(item => item.Value.Timer + ConnectedTimeout * TimeSpan.TicksPerMillisecond < DateTime.Now.Ticks), item =>{
                        RunOnMainThread(() => OnDisconnected?.Invoke(item.Key));
                        if(Sessions.TryRemove(item.Key, out _))
                            WriteLog($"{item.Key} Disconnected!", this, EnableLogs);
                    });

                    Thread.Sleep(1000);
                }
            }

            public void ChangeStatus(ServerStatus status, bool log = true){
                serverStatus = status;
                if(log)
                    WriteLog(status, this, EnableLogs);
                RunOnMainThread(() => OnStatus?.Invoke(status));
            }

            /// <summary>
            /// Clear all events, data and free memory.
            /// </summary>
            public void Dispose(){
                serverStatus = ServerStatus.Stopping;
                Socket?.Close();
                Socket = null;
                ListReceiveGroudIdPPS = new();
                Sessions.Clear();
                GC.SuppressFinalize(this);
                serverStatus = ServerStatus.Stopped;
            }
        }
    }
}