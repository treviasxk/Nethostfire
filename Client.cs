// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;

namespace Nethostfire {
    public class Client {
        static UdpClient MyClient;
        static IPEndPoint Host;
        static int PacketCount, PackTmp, TimeTmp;
        static long PingTmp, PingCount;
        static float PacketsReceived, PacketsSent;
        static string PublicKeyXML = "";
        static readonly ConcurrentQueue<Action> ListRunOnMainThread = new ConcurrentQueue<Action>();
        static ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        static Thread SendOnlineThread = new Thread(SendOnline), ClientReceiveUDPThread = new Thread(ClientReceiveUDP), CheckOnlineThread = new Thread(CheckOnline);
        /// <summary>
        /// O evento é chamado quando uma string é recebido de um Client e também será retornado uma string e o endereço IP do Client no parâmetro da função.
        /// </summary>
        public static Action<byte[], int> OnReceivedNewDataServer;
        /// <summary>
        /// O evento é chamado quando o status do client muda: Connected, Disconnected ou Connecting e também será retornado um StatusConnection no parâmetro da função.
        /// </summary>
        public static Action<ClientStatusConnection> OnClientStatusConnection;
        /// <summary>
        /// Estado atual do Client, Connected, Disconnected ou Connecting.
        /// </summary>

        public static ClientStatusConnection Status = ClientStatusConnection.Disconnected;
        /// <summary>
        /// Quantidade de pacotes recebido por segundo.
        /// </summary>
        public static string PacketsPerSeconds {get {return PacketCount +"pps";}}
        public static string PacketsSizeReceived {get {
                if(PacketsReceived > 1024000000)
                return (PacketsReceived / 1024000000).ToString("0.00") + "GB";
                if(PacketsReceived > 1024000)
                return (PacketsReceived / 1024000).ToString("0.00") + "MB";
                if(PacketsReceived > 1024)
                return (PacketsReceived / 1024).ToString("0.00") + "KB";
                if(PacketsReceived < 1024)
                return (PacketsReceived).ToString("0.00") + "Bytes";
                return "";
        }}
        public static string PacketsSizeSent {get {
                if(PacketsSent > 1000000000)
                return (PacketsSent / 1000000000).ToString("0.00") + "GB";
                if(PacketsSent > 1000000)
                return (PacketsSent / 1000000).ToString("0.00") + "MB";
                if(PacketsSent > 1000)
                return (PacketsSent / 1000).ToString("0.00") + "KB";
                if(PacketsSent < 1000)
                return (PacketsSent).ToString("0.00") + "Bytes";
                return "";
        }}
        public static string Ping {get {return PingCount + "ms";}}
        /// <summary>
        /// O modo Debug gera um arquivo de log "/Nethostfire_ErrorLogs.txt" e acrescente detalhes de um erro sempre que ocorre durante a execução.
        /// </summary>
        public static bool Debug {set { Resources.SaveLogError = value;}}
        /// <summary>
        /// Conecta no servidor com um IP e Porta especifico.
        /// </summary>
        public static void Connect(IPEndPoint _host){
            try{
                ChangeStatus(ClientStatusConnection.Connecting);
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
            }catch(Exception ex){
                DisconnectServer();
                Resources.AddLogError(ex);
            }
        }
        /// <summary>
        /// Deconecta o Client do servidor.
        /// </summary>
        public static void DisconnectServer(){
            if(Status == ClientStatusConnection.Connected){
                ChangeStatus(ClientStatusConnection.Disconnecting);
                try{
                    manualResetEvent.Reset();
                    PublicKeyXML = "";
                    Resources.SendPing(MyClient, new byte[]{0});
                }catch{
                    Thread.Sleep(3000);
                }
                ChangeStatus(ClientStatusConnection.Disconnected);
            }
        }
        /// <summary>
        ///  Envia uma string para o servidor.
        /// </summary>
        public static void SendBytes(byte[] _byte, int _hashCode){
            if(Status == ClientStatusConnection.Connected){
                Resources.Send(MyClient, _byte, _hashCode);
                PacketsSent += _byte.Length;
            }
        }
        /// <summary>
        ///  Executa ações dentro da thread principal do software, é utilizado para manipular objetos 3D na Unity.
        /// </summary>
        public static void RunOnMainThread(Action action){
            ListRunOnMainThread.Enqueue(action);
        }
        /// <summary>
        ///  Utilizado para definir a thread principal que irá executar as ações do RunOnMainThread(). Coloque essa ação dentro da função void Update() na Unity.
        /// </summary>
        public static void ThisMainThread(){
            if (!ListRunOnMainThread.IsEmpty) {
                while (ListRunOnMainThread.TryDequeue(out var action)){
                    action?.Invoke();
                }
            }
        }
        private static void ClientReceiveUDP(){
            while(true){
                if(MyClient != null)
                try{
                    byte[] data = MyClient.Receive(ref Host);
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
                                PublicKeyXML = "";
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
                        var _data = Resources.ByteToReceive(data);
                        if(PublicKeyXML == ""){
                            string _text = Encoding.UTF8.GetString(_data.Item1);
                            if(_text.StartsWith("<RSAKeyValue>") && _text.EndsWith("</RSAKeyValue>")){
                                PublicKeyXML = _text;
                                if(Status == ClientStatusConnection.Connecting){
                                    ChangeStatus(ClientStatusConnection.Connected);
                                    PingTmp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                }
                            }
                        }else{
                            OnReceivedNewDataServer?.Invoke(_data.Item1, _data.Item2);
                        }
                    }
                }catch(Exception ex){
                    Resources.AddLogError(ex);
                }
                manualResetEvent.WaitOne();
            }
        }
        static void SendOnline(){
            while(true){
                try{
                    if(Status == ClientStatusConnection.Connecting){
                        string _text = Resources.PublicKeyXML;
                        byte[] _byte  = Encoding.UTF8.GetBytes(Resources.PublicKeyXML);
                        Resources.Send(MyClient, _byte, _byte.GetHashCode());   
                    }
                    if(Status == ClientStatusConnection.Connected){
                        Resources.SendPing(MyClient, new byte[]{1});
                    }
                }catch{}
                Thread.Sleep(1000);
                manualResetEvent.WaitOne();
            }
        }
        static void CheckOnline(){
            while(true){
                if((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - PingTmp - 1000) > 3000 && Status == ClientStatusConnection.Connected){
                    ChangeStatus(ClientStatusConnection.Connecting);
                }
                manualResetEvent.WaitOne();
                Thread.Sleep(1000);
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