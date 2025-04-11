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
        public class Client : IDisposable{
            public int LimitPPS {get;set;} = 0;
            ConcurrentDictionary<int, int> ListSendGroudIdPPS = new();
            ConcurrentDictionary<int, int> ListReceiveGroudIdPPS = new();
            public Session session = new(){retransmissionBuffer = new(), Status = SessionStatus.Disconnected};
            public UdpClient? Socket;
            public SessionStatus Status {get{return session.Status;}}
            
            /// <summary>
            /// OnStatus is an event that returns ClientStatus whenever the status changes, with which you can use it to know the current status of the client.
            /// </summary>
            public Action<SessionStatus>? OnStatus;

            /// <summary>
            /// ConnectTimeout is the maximum time limit in milliseconds that a client can remain connected to the server when a ping is not received. (default value is 3000).
            /// </summary>
            public int ConnectTimeout {get; set;} = 3000;

            /// <summary>
            /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
            /// </summary>
            public bool EnableLogs {get; set;} = true;

            /// <summary>
            /// OnReceivedBytes an event that returns bytes received, GroupID and IP whenever the received bytes by clients, with it you can manipulate the bytes received.
            /// </summary>
            public Action<byte[], int>? OnReceivedBytes;

            public void Connect(IPAddress Host, int Port, int symmetricSizeRSA = 86){
                try{
                    if(Socket == null){
                        Socket = new UdpClient();
                        StartUnity(client: this);
                        GenerateKey(symmetricSizeRSA);
                        WriteLog($"Connecting on {Host}:{Port}", this, EnableLogs);
                        ChangeStatus(SessionStatus.Connecting, false);
                        session.Status = SessionStatus.Connecting;
                        Socket.Connect(new IPEndPoint(Host, Port));
                        new Thread(ReceivePacket){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                        new Thread(Service){
                            IsBackground = true,
                            Priority = ThreadPriority.Highest
                        }.Start();
                    }
                }catch(Exception ex){
                    throw new Nethostfire(ex.Message, this);
                }
            }

            public void Send(byte[]? bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => SendPacket(Socket, ref bytes, groupID, typeEncrypt, typeShipping, ref session, ListSendGroupIdPPS: in ListSendGroudIdPPS);
            public void Send(string text, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(Encoding.UTF8.GetBytes(text), groupID, typeEncrypt, typeShipping);
            public void Send(int value, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(BitConverter.GetBytes(value), groupID, typeEncrypt, typeShipping);
            public void Send(object data, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None) => Send(Json.GetBytes(data), groupID, typeEncrypt, typeShipping);

            public void SetSendLimitGroupPPS(int groupID, int pps){
                if(pps > 0)
                    ListSendGroudIdPPS.TryAdd(groupID, pps);
                else
                    ListSendGroudIdPPS.TryRemove(groupID, out _);
            }

            public void SetReceiveLimitGroupPPS(int groupID, int pps){
                if(pps > 0)
                    ListReceiveGroudIdPPS.TryAdd(groupID, pps);
                else
                    ListReceiveGroudIdPPS.TryRemove(groupID, out _);
            }

            public void ChangeStatus(SessionStatus status, bool log = true){
                session.Status = status;
                if(log)
                    WriteLog(status, this, EnableLogs);
                RunOnMainThread(() => OnStatus?.Invoke(status));
            }

            public void Disconnect(){
                ChangeStatus(SessionStatus.Disconnecting);
                Dispose();
                ChangeStatus(SessionStatus.Disconnected);
            }

            async void ReceivePacket(){
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
                    
                    if(bytes != null)
                    Parallel.Invoke(()=>{
                        // Update ping and timer connection
                        if(bytes.Length == 1){
                            switch(bytes[0]){
                                case 0: // Kicked from server
                                    //ChangeStatus(ClientStatus.Kicked);
                                return;
                                case 1: // Update ping
                                    session.Ping = GetPing(session.Timer);
                                    session.Timer = DateTime.Now.Ticks;
                                return;
                                case 2: // Max client exceeded
                                    //ChangeStatus(ClientStatus.MaxClientExceeded);
                                return;
                                case 3: // IP blocked
                                    //ChangeStatus(ClientStatus.IpBlocked);
                                return;
                            }
                        }

                        // item1 = bytes
                        // item2 = groupID
                        // item3 = typeEncrypt
                        // item4 = typeShipping
                        var data = DeconvertPacket(Socket, bytes, ref session, null);
                        if(data.HasValue)
                        if(Status == SessionStatus.Connected){
                            if(CheckReceiveLimitPPS(data.Value.Item1, data.Value.Item2, ref session, LimitPPS, in ListReceiveGroudIdPPS))
                                RunOnMainThread(() =>OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2));
                            return;
                        }else
                        if(Status == SessionStatus.Connecting){
                            // Update timer
                            session.Timer = DateTime.Now.Ticks;
                            // Check RSA server and send AES client
                            if(data.Value.Item2 == 0){
                                string value = Encoding.ASCII.GetString(data.Value.Item1);
                                if(value.StartsWith("<RSAKeyValue>") && value.EndsWith("</RSAKeyValue>") && PrivateKeyAES != null)
                                    session.PublicKeyRSA = value;
                                return;
                            }else
                            // Check AES server and connect
                            if(data.Value.Item2 == 1){
                                if(data.Value.Item1.Length == 16){
                                    session.PrivateKeyAES = data.Value.Item1;
                                    ChangeStatus(SessionStatus.Connected);
                                }
                                return;
                            }
                        }    
                    });
                }
            }


            void Service(){
                while(Status != SessionStatus.Disconnected){
                    // Authenting
                    if(Status == SessionStatus.Connecting){
                        if(session.PublicKeyRSA == ""){
                            var bytes = Encoding.ASCII.GetBytes(PublicKeyRSA!);
                            SendPacket(Socket!, ref bytes, 0, TypeEncrypt.Compress, TypeShipping.None, ref session);
                        }else
                        if(session.PrivateKeyAES == null){
                            var key = PrivateKeyAES;
                            SendPacket(Socket!, ref key, 1, TypeEncrypt.RSA, TypeShipping.None, ref session);
                        }
                    }

                    if(Status == SessionStatus.Connected){
                        SendPing(Socket, [1]);  // Send Online Status
                        // Set Connecting...
                        if(session.Timer + ConnectTimeout * TimeSpan.TicksPerMillisecond < DateTime.Now.Ticks){
                            session.Status = SessionStatus.Connecting;
                            session.PublicKeyRSA = "";
                            session.PrivateKeyAES = null;
                            ChangeStatus(SessionStatus.Connecting);
                        }
                    }
                    
                    // Packets retranmission
                    Parallel.ForEach(session.retransmissionBuffer.Values, bytes => SendPing(Socket, bytes));
                    Thread.Sleep(1000);
                }
            }

            /// <summary>
            /// Clear all events, data and free memory.
            /// </summary>
            public void Dispose(){
                // Is need clear events first to can clean ListRunMainThread in NethostfireService
                session.Status = SessionStatus.Disconnecting;
                Socket?.Close();
                Socket = null;
                ListSendGroudIdPPS = new();
                session = new(){retransmissionBuffer = new(), Status = SessionStatus.Disconnected};
                GC.SuppressFinalize(this);
                session.Status = SessionStatus.Disconnected;
            }
        }
    }
}