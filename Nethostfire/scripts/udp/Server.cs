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

namespace Nethostfire {
    public partial class UDP{
        public class Server : IDisposable{
            public UdpClient? Socket;
            ServerStatus serverStatus = ServerStatus.Stopped;

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
            public ICollection<IPEndPoint> Clients {get{return Sessions.data.Keys;}}

            /// <summary>
            /// OnStatus is an event that returns ServerStatus whenever the status changes, with which you can use it to know the current status of the server.
            /// </summary>
            public Action<ServerStatus>? OnStatus;
            
            /// <summary>
            /// OnReceivedBytes an event that returns bytes received, GroupID and IP whenever the received bytes by clients, with it you can manipulate the bytes received.
            /// </summary>
            public Action<byte[], int, IPEndPoint>? OnReceivedBytes;

            public ServerStatus Status {get {return serverStatus;}}

            Sessions Sessions = new();

            public void Start(IPAddress Host, Int16 Port, int symmetricSizeRSA = 86){
                try{
                    if(Socket == null){
                        Socket ??= new UdpClient();
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

            async void ReceivePacket(){
                while(Socket != null){
                    byte[]? bytes;
                    IPEndPoint? ip;
                    Session session = new();

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
                        var Authenticated = Sessions.TryGetOrUpdateValue(ip, ref session);
                        var data = DeconvertPacket(bytes, ref session);

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
                                        Sessions.TryUpdate(ip, in session);
                                        SendPing(Socket, [1], ip);
                                    return;
                                }
                            }else
                            if(data.HasValue){                  
                                if(session.Credentials.PublicKeyRSA == null || session.Credentials.PrivateKeyAES == null){
                                    switch(data.Value.Item2){
                                        case 0:
                                            // Resend RSA
                                            SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA!), 0, TypeEncrypt.None, ref session, ip);
                                        return;
                                        case 1:
                                            // AES
                                            session.Credentials.PrivateKeyAES = data.Value.Item1;
                                            WriteLog($"{ip} Connected!", this, EnableLogs);
                                            SendPacket(Socket, PrivateKeyAES!, 1, TypeEncrypt.RSA, ref session, ip);
                                            Sessions.TryUpdate(ip, in session);
                                        return;
                                    }
                                }else{
                                    OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2, ip);
                                }
                            }
                        }else{
                            // Check Authenticated
                            if(data != null && data.Value.Item2 == 0){  // groupID = 0
                                string value = Encoding.ASCII.GetString(data.Value.Item1);
                                if(value.StartsWith("<RSAKeyValue>") && value.EndsWith("</RSAKeyValue>")){
                                    session.Credentials.PublicKeyRSA = value;
                                    session.Timer = DateTime.Now.Ticks;
                                    if(Sessions.TryAdd(ip, session)){
                                        WriteLog($"{ip} Incomming...", this, EnableLogs);
                                        SendPacket(Socket, Encoding.ASCII.GetBytes(PublicKeyRSA!), 0, TypeEncrypt.Compress, ref session, ip);
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
                while(Status == ServerStatus.Running){
                    // Check timer connection dataClients.
                    Parallel.ForEach(Sessions.data.Where(item => item.Value.Timer + ConnectedTimeout * TimeSpan.TicksPerMillisecond < DateTime.Now.Ticks), item =>{
                        //OnDisconnected?.Invoke(item.Key);
                        if(Sessions.TryRemove(item.Key))
                            WriteLog($"{item.Key} Disconnected!", this, EnableLogs);
                    });
                    Thread.Sleep(1000);
                }
            }

            public void ChangeStatus(ServerStatus status, bool log = true){
                serverStatus = status;
                if(log)
                    WriteLog(status, this, EnableLogs);
                OnStatus?.Invoke(status);
            }

            /// <summary>
            /// Clear all events, data and free memory.
            /// </summary>
            public void Dispose(){
                serverStatus = ServerStatus.Stopping;
                Socket?.Close();
                Socket = null;
                Sessions.Clear();
                GC.SuppressFinalize(this);
                serverStatus = ServerStatus.Stopped;
            }
        }
    }
}