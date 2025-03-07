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
        public class Client : IDisposable{
            ClientStatus clientStatus = ClientStatus.Disconnected;
            Session session = new(){retransmissionBuffer = new()};
            public UdpClient? Socket;
            public ClientStatus Status {get{return clientStatus;}}
            
            /// <summary>
            /// OnStatus is an event that returns ClientStatus whenever the status changes, with which you can use it to know the current status of the client.
            /// </summary>
            public Action<ClientStatus>? OnStatus;

            /// <summary>
            /// ConnectTimeout is the maximum time limit in milliseconds that a client can remain connected to the server when a ping is not received. (default value is 3000).
            /// </summary>
            public int ConnectTimeout {get; set;} = 3000;

            /// <summary>
            /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
            /// </summary>
            public bool EnableLogs {get; set;} = true;
            public void Connect(IPAddress Host, int Port, int symmetricSizeRSA = 86){
                try{
                    if(Socket == null){
                        Socket = new UdpClient();
                        GenerateKey(symmetricSizeRSA);
                        WriteLog($"Connecting on {Host}:{Port}", this, EnableLogs);
                        ChangeStatus(ClientStatus.Connecting, false);
                        clientStatus = ClientStatus.Connecting;
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


            public void ChangeStatus(ClientStatus status, bool log = true){
                clientStatus = status;
                if(log)
                    WriteLog(status, this, EnableLogs);
                OnStatus?.Invoke(status);
            }

            public void Send(byte[] bytes, int groupID, TypeEncrypt typeEncrypt = TypeEncrypt.None){
                SendPacket(Socket, bytes, groupID, typeEncrypt, ref session);
            }

            public void Disconnect(){
                ChangeStatus(ClientStatus.Disconnecting);
                Dispose();
                ChangeStatus(ClientStatus.Disconnected);
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
                        var data = DeconvertPacket(bytes, ref session);
                        if(data.HasValue)
                        if(Status == ClientStatus.Connected){
                            
                            //RunOnMainThread(() => OnReceivedBytes?.Invoke(data.Value.Item1, data.Value.Item2));
                        }else
                        if(Status == ClientStatus.Connecting){
                            // Check RSA server and send AES client
                            if(data.Value.Item2 == 0){
                                string value = Encoding.ASCII.GetString(data.Value.Item1);
                                if(value.StartsWith("<RSAKeyValue>") && value.EndsWith("</RSAKeyValue>") && PrivateKeyAES != null)
                                    session.Credentials.PublicKeyRSA = value;
                                return;
                            }else
                            // Check AES server and connect
                            if(data.Value.Item2 == 1){
                                if(data.Value.Item1.Length == 16){
                                    session.Credentials.PrivateKeyAES = data.Value.Item1;
                                    session.Timer = DateTime.Now.Ticks;
                                    ChangeStatus(ClientStatus.Connected);
                                }
                                return;
                            }
                        }    

                    });
                }
            }


            void Service(){
                while(Status != ClientStatus.Disconnected){
                    // Authenting
                    if(Status == ClientStatus.Connecting){
                        if(session.Credentials.PublicKeyRSA == null){
                            SendPacket(Socket!, Encoding.ASCII.GetBytes(PublicKeyRSA!), 0, TypeEncrypt.Compress, ref session);
                        }else
                        if(session.Credentials.PrivateKeyAES == null)
                            SendPacket(Socket!, PrivateKeyAES!, 1, TypeEncrypt.RSA, ref session);
                    }

                    if(Status == ClientStatus.Connected){
                        SendPing(Socket, [1]);  // Send Online Status
                        if(session.Timer + ConnectTimeout * TimeSpan.TicksPerMillisecond < DateTime.Now.Ticks){
                            clientStatus = ClientStatus.Connecting;
                            session.Credentials = new();
                            ChangeStatus(ClientStatus.Connecting);
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
                clientStatus = ClientStatus.Disconnecting;
                Socket?.Close();
                Socket = null;
                session = new(){retransmissionBuffer = new()};
                GC.SuppressFinalize(this);
                clientStatus = ClientStatus.Disconnected;
            }
        }
    }
}