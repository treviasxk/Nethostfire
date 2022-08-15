// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nethostfire {
    public class Client {
        static UdpClient MyClient;
        static IPEndPoint Host;
        static int PacketCount, PackTmp, TimeTmp, connectTimeOut = 10000, waitConnectionHold = 1000;
        static long PingTmp, PingCount;
        static float PacketsReceived, PacketsSent;
        static ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        static Dictionary<int, HoldConnection> ListHoldConnection = new Dictionary<int, HoldConnection>();
        static Thread SendOnlineThread = new Thread(SendOnline), ClientReceiveUDPThread = new Thread(ClientReceiveUDP), CheckOnlineThread = new Thread(CheckOnline);
        /// <summary>
        /// Chave publica de criptografia RSA.
        /// </summary>
        public static string PublicKeyXML {get;set;}
        /// <summary>
        /// O evento é chamado quando bytes é recebido do server.
        /// </summary>
        public static Action<byte[], int> OnReceivedNewDataServer;
        /// <summary>
        /// O evento é chamado quando o status do client muda.
        /// </summary>
        public static Action<ClientStatusConnection> OnClientStatusConnection;
        /// <summary>
        /// Estado atual do Client.
        /// </summary>
        public static ClientStatusConnection Status {get;set;} = ClientStatusConnection.Disconnected;
        /// <summary>
        /// Quantidade de pacotes recebido por segundo (pps).
        /// </summary>
        public static string PacketsPerSeconds {get {return PacketCount +"pps";}}
        /// <summary>
        /// Tempo limite de reconexão, depois que esgotar o Status mudará para NoConnection, o valor padrão é 10000 (ms), definir para 0 a reconexão será infinito.
        /// </summary>
        public static int ConnectTimeOut {get {return connectTimeOut;} set{connectTimeOut = value;}}
        /// <summary>
        /// Tempo de bloqueio para evitar duplos bytes recebidos do mesmo hashcode durante um pacote com o HoldConnection ligado. O valor padrão é 1000 (ms).
        /// </summary>
        public static int WaitConnectionHold {get {return waitConnectionHold;} set{waitConnectionHold = value;}}
        /// <summary>
        /// Tamanho total de pacotes recebido.
        /// </summary>
        public static string PacketsSizeReceived {get {return Resources.BytesToString(PacketsReceived);}}
        /// <summary>
        /// Tamanho total de pacotes enviado.
        /// </summary>
        public static string PacketsSizeSent {get {return Resources.BytesToString(PacketsSent);}}
        /// <summary>
        /// Agrupador de Pacotes da Internet, ping (ms).
        /// </summary>
        public static string Ping {get {return PingCount + "ms";}}
        /// <summary>
        /// Conecta no servidor com um IP e Porta especifico.
        /// </summary>
        public static void Connect(IPEndPoint _host){
            ChangeStatus(ClientStatusConnection.Connecting);
            ListHoldConnection.Clear();
            PingTmp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            try{
                if(MyClient is null){
                    MyClient = new UdpClient();
                    Resources.GenerateKeyRSA();
                    Host = _host;
                    MyClient.Connect(Host);
                    ClientReceiveUDPThread.IsBackground = true;
                    SendOnlineThread.IsBackground = true;
                    CheckOnlineThread.IsBackground = true;
                    ClientReceiveUDPThread.Start();
                    SendOnlineThread.Start();
                    CheckOnlineThread.Start();
                }else{
                    manualResetEvent.Set();
                }
            }catch{
                DisconnectServer();
            }
        }
        /// <summary>
        /// Deconecta o Client do servidor.
        /// </summary>
        public static void DisconnectServer(){
            if(Status == ClientStatusConnection.Connected){
                ChangeStatus(ClientStatusConnection.Disconnecting);
                if(!Resources.SendPing(MyClient, new byte[]{0}))
                    Thread.Sleep(3000);
            }
            manualResetEvent.Reset();
            ListHoldConnection.Clear();
            if(Status == ClientStatusConnection.Disconnecting)
                ChangeStatus(ClientStatusConnection.Disconnected);
            if(Status == ClientStatusConnection.Connecting)
                ChangeStatus(ClientStatusConnection.NoConnection);
        }
        /// <summary>
        /// Envie bytes para o servidor.
        /// </summary>
        public static void SendBytes(byte[] _byte, int _hashCode, bool _holdConnection = false){
            if(Status == ClientStatusConnection.Connected){
                if(_holdConnection && !ListHoldConnection.ContainsKey(_hashCode))
                    ListHoldConnection.Add(_hashCode, new HoldConnection{Bytes = _byte, Time = 0});
                Resources.Send(MyClient, _byte, _hashCode, _holdConnection);
                PacketsSent += _byte.Length;
            }
        }

        private static void ClientReceiveUDP(){
            if(MyClient != null)
            while(true){
                byte[] data = null;
                try{
                    data = MyClient.Receive(ref Host);
                }catch{}

                if(data != null){
                    Parallel.Invoke(()=>{
                        PackTmp++;
                        if(DateTime.Now.Second != TimeTmp){
                            TimeTmp = DateTime.Now.Second;
                            PacketCount = PackTmp;
                            PackTmp = 0;
                        }

                        if(data.Length == 1){
                            switch(data[0]){
                                case 0:
                                    manualResetEvent.Reset();
                                    ChangeStatus(ClientStatusConnection.Disconnected);
                                break;
                                case 1:
                                    PingCount = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - PingTmp - 1000;
                                    PingTmp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                break;
                            }
                        }


                        if(data.Length > 1){
                            PacketsReceived += data.Length;
                            var _data = Resources.ByteToReceive(data, MyClient);
                            if(_data.Item3){
                                ListHoldConnection.Remove(_data.Item2);
                            }
                            else
                            if(Status == ClientStatusConnection.Connecting){
                                string _text = Encoding.UTF8.GetString(_data.Item1);
                                if(_text.StartsWith("<RSAKeyValue>") && _text.EndsWith("</RSAKeyValue>")){
                                    PublicKeyXML = _text;
                                    PingTmp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                    ChangeStatus(ClientStatusConnection.Connected);
                                }
                            }else{
                                if(ListHoldConnection.ContainsKey(_data.Item2)){
                                    if(ListHoldConnection[_data.Item2].Time == 0){
                                        ListHoldConnection[_data.Item2].Time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + waitConnectionHold;
                                        if(PublicKeyXML != "")
                                            OnReceivedNewDataServer?.Invoke(_data.Item1, _data.Item2);
                                    }else{
                                        if(ListHoldConnection[_data.Item2].Time < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond){
                                            ListHoldConnection.Remove(_data.Item2);
                                        }
                                    }
                                }else{
                                    if(PublicKeyXML != "")
                                        OnReceivedNewDataServer?.Invoke(_data.Item1, _data.Item2);
                                }
                            }
                        }
                    });

                }
                manualResetEvent.WaitOne();
            }
        }
        static void SendOnline(){
            while(true){
                if(Status == ClientStatusConnection.Connecting){
                    byte[] _byte  = Encoding.UTF8.GetBytes(Resources.PublicKeyXML);
                    Resources.Send(MyClient, _byte, _byte.GetHashCode(), false);   
                }
                if(Status == ClientStatusConnection.Connected){
                    Resources.SendPing(MyClient, new byte[]{1});
                    Dictionary<int, HoldConnection> x = new Dictionary<int, HoldConnection>();
                    Parallel.ForEach(ListHoldConnection.ToArray(), item => {
                        Resources.Send(MyClient, item.Value.Bytes, item.Key, false);
                    });
                }
                Thread.Sleep(1000);
                manualResetEvent.WaitOne();
            }
        }
        static void CheckOnline(){
            while(true){
                if((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - PingTmp - 1000) > 3000 && Status == ClientStatusConnection.Connected){
                    ChangeStatus(ClientStatusConnection.Connecting);
                    ListHoldConnection.Clear();
                }
                if(Status == ClientStatusConnection.Connecting && (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - PingTmp) > 3000 + connectTimeOut && connectTimeOut != 0)
                    DisconnectServer();
                Thread.Sleep(1000);
                manualResetEvent.WaitOne();
            }
        }
        static void ChangeStatus(ClientStatusConnection _status){
            if(Status != _status){
                Status = _status;
                OnClientStatusConnection?.Invoke(_status);
            }
        }
    }
}