// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.Text;
using static Nethostfire.Nethostfire;
using static Nethostfire.DataSecurity;
using System.Collections.Concurrent;
using static Nethostfire.UDP.UDP;

namespace Nethostfire.UDP {
    public class Server : IDisposable{
        public UdpClient? Socket {get;set;}
        public int LimitPPS {get;set;} = 0;
        ServerState serverStatus = ServerState.Stopped;
        ConcurrentDictionary<int, int>? ListReceiveGroudIdPPS;
        ConcurrentDictionary<int, int>? ListSendGroudIdPPS;
        /// <summary>
        /// ConnectTimeout is the maximum time limit in milliseconds that a client can remain connected to the server when a ping is not received. (default value is 3000)
        /// </summary>
        public int ConnectedTimeout {get; set;} = 3000;

        /// <summary>
        /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
        /// </summary>
        public bool EnableLogs {get; set;} = true;

        /// <summary>
        /// The Sessions is all clients connected to the server.
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Session> Sessions { get; set; } = new();

        /// <summary>
        /// StateChanged is an event that returns ServerStatus whenever the status changes, with which you can use it to know the current status of the server.
        /// </summary>
        public event EventHandler<ServerStateEventArgs>? StateChanged;
        
        /// <summary>
        /// DataReceived an event that returns bytes received, GroupID and IP whenever the received bytes by clients, with it you can manipulate the bytes received.
        /// </summary>
        public event EventHandler<ServerDataReceivedEventArgs>? DataReceived;

        /// <summary>
        /// Connected is an event that you can use to receive the IP and Session whenever a session connected.
        /// </summary>
        public event EventHandler<SessionEventArgs>? Connected;

        /// <summary>
        /// Disconnected is an event that you can use to receive the IP and Session whenever a session disconnected.
        /// </summary>
        public event EventHandler<SessionEventArgs>? Disconnected;

        public ServerState State {get {return serverStatus;}}
        public Server(IPAddress? Host = null, short Port = 0, int symmetricSizeRSA = 86)
        {
            StartUnity();
            Sessions ??= new();
            ListReceiveGroudIdPPS ??= new();
            ListSendGroudIdPPS ??= new();
            if (Host != null)
                Start(IPAddress.Any, Port, symmetricSizeRSA);
        }


        public void Start(IPAddress Host, short Port = 0, int symmetricSizeRSA = 86){
            try{
                if(Socket == null){
                    Socket ??= new UdpClient();
                    GenerateKey(symmetricSizeRSA);
                    ChangeStatus(ServerState.Initializing);
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
                    ChangeStatus(ServerState.Running, false);
                }
            }catch(Nethostfire ex){
                throw new Nethostfire(ex.Message, this);
            }
        }

        public void SetReceiveLimitGroupPPS(ushort groupID, int pps){
            if(pps > 0)
                ListReceiveGroudIdPPS!.TryAdd(groupID, pps);
            else
                ListReceiveGroudIdPPS!.TryRemove(groupID, out _);
        }

        public void SetSendLimitGroupPPS(ushort groupID, int pps){
            if(pps > 0)
                ListSendGroudIdPPS!.TryAdd(groupID, pps);
            else
                ListSendGroudIdPPS!.TryRemove(groupID, out _);
        }

        public void Send(byte[]? bytes, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None){
            if(Sessions.TryGetValue(ip, out var session))
                SendPacket(Socket, ref bytes, groupID, typeEncrypt, typeShipping, ref session, ip, in ListSendGroudIdPPS);
        }
        
        public void Send(string text, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(Encoding.UTF8.GetBytes(text), groupID, ip, typeEncrypt, typeShipping);
        public void Send(int value, int groupID, IPEndPoint ip, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(BitConverter.GetBytes(value), groupID, ip, typeEncrypt, typeShipping);
        public void SendGroup(byte[]? bytes, int groupID, ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Parallel.ForEach(ips, (ip) => Send(bytes, groupID, ip, typeEncrypt, typeShipping));
        public void SendGroup(string text, int groupID, ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroup(Encoding.UTF8.GetBytes(text), groupID, ips, typeEncrypt, typeShipping);
        public void SendGroup(int value, int groupID, ConcurrentQueue<IPEndPoint> ips, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendGroup(BitConverter.GetBytes(value), groupID, ips, typeEncrypt, typeShipping);

        public void SendAll(byte[]? bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Parallel.ForEach(Sessions.Keys, (ip) => Send(bytes, groupID, ip, typeEncrypt, typeShipping));
        public void SendAll(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAll(Encoding.UTF8.GetBytes(text), groupID, typeEncrypt, typeShipping);
        public void SendAll(int value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendAll(BitConverter.GetBytes(value), groupID, typeEncrypt, typeShipping);

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
                        session.Timer = DateTime.Now.Ticks;
                        // Commands
                        if (bytes.Length == 1)
                        {
                            switch (bytes[0])
                            {
                                case 0: // Force disconnect
                                        //Kick(ip);
                                    return;
                                case 1: // Update ping
                                    session.Ping = GetPing(session.TimerPing);
                                    session.TimerPing = DateTime.Now.Ticks;
                                    SendPing(Socket, [1], ip);
                                    return;
                                case 2:
                                    SendPing(Socket, [1], ip);
                                    return;
                            }
                        }
                        else
                        {
                            if (session.PublicKeyRSA == "" || session.PrivateKeyAES == null)
                            {
                                switch (data.Value.Item2)
                                {
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
                                        if (session.Status == SessionStatus.Connecting)
                                        {
                                            WriteLog($"{ip} Connected!", this, EnableLogs);
                                            session.Status = SessionStatus.Connected;
                                            RunParallel(()=>Connected?.Invoke(this, new SessionEventArgs(ip, session)));
                                        }
                                        return;
                                }
                            }
                            else
                            {
                                if (CheckReceiveLimitPPS(data.Value.Item1, data.Value.Item2, ref session, LimitPPS, in ListReceiveGroudIdPPS))
                                   RunParallel(()=>DataReceived?.Invoke(this, new ServerDataReceivedEventArgs(this, data.Value.Item1, data.Value.Item2, ip)));
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
            ChangeStatus(ServerState.Stopping);
            Dispose();
            ChangeStatus(ServerState.Stopped);
        }

        void Service(){
            while(Socket != null){
                // Check timer connection dataClients.
                Parallel.ForEach(Sessions.Where(item => item.Value.Timer + ConnectedTimeout * TimeSpan.TicksPerMillisecond < DateTime.Now.Ticks), item =>{
                    if(Sessions.TryRemove(item.Key, out _)){
                        WriteLog($"{item.Key} Disconnected!", this, EnableLogs);
                        RunParallel(()=>Disconnected?.Invoke(this, new SessionEventArgs(item.Key, item.Value)));
                    }
                });

                // Packets retranmission
                Parallel.ForEach(Sessions.Values.Where(item => item.retransmissionBuffer.Count > 0), session
                    => Parallel.ForEach(session.retransmissionBuffer.Values, (bytes) => SendPing(Socket, bytes)));
                Thread.Sleep(1000);
            }
        }

        internal void ChangeStatus(ServerState status, bool log = true){
            serverStatus = status;
            if(log)
                WriteLog(status, this, EnableLogs);
            RunParallel(() => StateChanged?.Invoke(this, new ServerStateEventArgs(status)));
        }

        /// <summary>
        /// Clear all events, data and free memory.
        /// </summary>
        public void Dispose(){
            DataReceived = null;
            StateChanged = null;
            Connected = null;
            Disconnected = null;
            Socket?.Close();
            Socket = null;
            ListReceiveGroudIdPPS = null;
            ListSendGroudIdPPS = null;
            Sessions = new();
            serverStatus = ServerState.Stopped;
            GC.SuppressFinalize(this);
        }
    }
}