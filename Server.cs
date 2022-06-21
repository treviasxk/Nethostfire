// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;

namespace Nethostfire {
   public class Server {
      static UdpClient MyServer;
      static IPEndPoint Host;
      static int PacketCount, PackTmp, TimeTmp;
      static float PacketsReceived, PacketsSent;
      static readonly ConcurrentQueue<Action> ListRunOnMainThread = new ConcurrentQueue<Action>();
      static ManualResetEvent manualResetEvent = new ManualResetEvent(true);
      static  Thread CheckOnlineThread = new Thread(CheckOnline), ServerReceiveUDPThread = new Thread(ServerReceiveUDP);
      /// <summary>
      /// O evento é chamado quando uma string é recebido de um Client e também será retornado uma string e o endereço IP do Client no parâmetro da função.
      /// </summary>
      public static event Eventos.OnReceivedNewDataClient OnReceivedNewDataClient;
        /// <summary>
        /// O evento é chamado quando o status do servidor muda: Stopped ou Running e também será retornado um ServerStatusConnection no parâmetro da função.
        /// </summary>
      public static event Eventos.OnServerStatusConnection OnServerStatusConnection;
      /// <summary>
      /// O evento é chamado quando um Client é conectado no servidor.
      /// </summary>
      public static event Eventos.OnConnectedClient OnConnectedClient;
      /// <summary>
      /// O evento é chamado quando um Client se conecta no servidor e também é retornado o endereço IP do Client.
      /// </summary>
      public static event Eventos.OnDisconnectedClient OnDisconnectedClient;
      /// <summary>
      /// Lista de todos os Clients que estão conectado no servidor.
      /// </summary>
      static List<DataClient> DataClients = new List<DataClient>();
      /// <summary>
      /// Estado atual do servidor, Connected, Disconnected ou Reconnecting.
      /// </summary>
      public static ServerStatusConnection Status {get;set;} = ServerStatusConnection.Stopped;
      /// <summary>
      /// Número total de Clients conectado
      /// </summary>
      public static int ClientsConnected {get {return DataClients.Count;}}
      /// <summary>
      /// Quantidade de pacotes recebido por segundo.
      /// </summary>
      public static string PacketsPerSeconds {get {return PackTmp +"pps";}}
      public static string PacketsSizeReceived {get {
            if(PacketsReceived > 1000000000)
            return (PacketsReceived / 1000000000).ToString("0.00") + "GB";
            if(PacketsReceived > 1000000)
            return (PacketsReceived / 1000000).ToString("0.00") + "MB";
            if(PacketsReceived > 1000)
            return (PacketsReceived / 1000).ToString("0.00") + "KB";
            if(PacketsReceived < 1000)
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
      /// <summary>
      /// O modo Debug gera um arquivo de log "/Nethostfire_ErrorLogs.txt" e acrescente detalhes de um erro sempre que ocorre durante a execução.
      /// </summary>
      public static bool Debug {set { Resources.SaveLogError = value;}}
      /// <summary>
      /// Inicia o servidor com um IP e Porta especifico, em _encrypy você pode definir se a conexão é criptografado com RSA.
      /// </summary>
      public static void Start(IPEndPoint _host, bool _encrypt = true){
         try{
            if(MyServer is null){
               MyServer = new UdpClient();
               Resources.GenerateKeyRSA();
               Host = _host;
               MyServer.Client.Bind(Host);
               ServerReceiveUDPThread.IsBackground = true;
               CheckOnlineThread.IsBackground = true;
               ServerReceiveUDPThread.Start();
               CheckOnlineThread.Start();
            }else{
               manualResetEvent.Set();
            }
            ChangeStatus(ServerStatusConnection.Running);
         }catch(Exception ex){
            Resources.AddLogError(ex);
         }
      }
      /// <summary>
      /// Pará o servidor. (Todos os Clients serão desconectados)
      /// </summary>
      public static void Stop(){
         if(Status == ServerStatusConnection.Running){
            manualResetEvent.Reset();
            Thread.Sleep(3000);
            DisconnectClientAll();
            Resources.GenerateKeyRSA();
            Thread.Sleep(2000);
            ChangeStatus(ServerStatusConnection.Stopped);
         }
      }
      /// Reinicia o servidor. (Todos os Clients serão desconectados)
      public static void Reset(){
         Stop();
         Start(Host);
      }
      /// <summary>
      ///  Envia uma string para o servidor.
      /// </summary>
      public static void SendBytes(byte[] _byte, Type _type, DataClient _dataClient, bool _encrypt = false){
         try{
            if(Status == ServerStatusConnection.Running){
               byte[] buffer = Resources.ByteToSend(_byte, _type, _encrypt);
               MyServer.Send(buffer, buffer.Length, _dataClient.IP);
               PacketsSent += buffer.Length;
            }
         }catch{}
      }
      /// <summary>
      ///  Envie a string para um grupo de Clients conectado no servidor.
      /// </summary>
      public static void SendBytesGroup(byte[] _byte, Type _type, List<DataClient> _dataClients, bool _encrypt = false){
         foreach(DataClient _dataClient in _dataClients){
            try{
               if(Status == ServerStatusConnection.Running){
                  byte[] buffer = Resources.ByteToSend(_byte, _type, _encrypt);
                  MyServer.Send(buffer, buffer.Length, _dataClient.IP);
                  PacketsSent += buffer.Length;
               }
            }catch{}
         }
      }
      /// <summary>
      ///  Envie a string para todos os Clients conectado no servidor.
      /// </summary>
      public static void SendBytesAll(byte[] _byte, Type _type, bool _encrypt = false){
         foreach(DataClient _dataClient in DataClients){
            try{
               if(Status == ServerStatusConnection.Running){
                  byte[] buffer = Resources.ByteToSend(_byte, _type, _encrypt);
                  MyServer.Send(buffer, buffer.Length, _dataClient.IP);
                  PacketsSent += buffer.Length;
               }
            }catch{}
         }
      }
      /// <summary>
      /// Deconecta um Client especifico do servidor.
      /// </summary>
      public static void DisconnectClient(DataClient _dataClient){
         try{
            if(Status == ServerStatusConnection.Running){
               byte[] buffer = new byte[] {0};
               MyServer.Send(buffer, buffer.Length, _dataClient.IP);
            }
         }catch{}
      }
      /// <summary>
      /// Deconecta um grupo de Clients do servidor.
      /// </summary>
      public static void DisconnectClientGroup(List<DataClient> _DataClients){
         foreach(DataClient _dataClient in _DataClients){
            try{
               if(Status == ServerStatusConnection.Running){
                  byte[] buffer = new byte[] {0};
                  MyServer.Send(buffer, buffer.Length, _dataClient.IP);
               }
            }catch{}
         }
      }
      /// <summary>
      /// Deconecta todos os Clients conectado no servidor.
      /// </summary>
      public static void DisconnectClientAll(){
         foreach(DataClient _dataClient in DataClients){
            try{
               if(Status == ServerStatusConnection.Running){
                  byte[] buffer = new byte[] {0};
                  MyServer.Send(buffer, buffer.Length, _dataClient.IP);
               }
            }catch{}
         }
         DataClients.Clear();
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
      public static void ThisMainThread() {
         if (!ListRunOnMainThread.IsEmpty) {
            while (ListRunOnMainThread.TryDequeue(out var action)) {
               action?.Invoke();
            }
         }
      }
      static void ServerReceiveUDP(){
         while(true){
            try{
               IPEndPoint _ip = new IPEndPoint(IPAddress.Any, 0);
               byte[] data = MyServer.Receive(ref _ip);
               PacketsReceived += data.Length;
               PacketCount++;
               if(DateTime.Now.Second != TimeTmp){
                  TimeTmp = DateTime.Now.Second;
                  PackTmp = PacketCount;
                  PacketCount = 0;
               }
               DataClient _dataClient = new DataClient();
               if(DataClients.Any(DataClient => DataClient.IP.ToString() == _ip.ToString())){
                  int index = DataClients.FindIndex(DataClient => DataClient.IP.ToString() == _ip.ToString());
                  _dataClient = DataClients.ElementAt(index);
                  if(data.Length == 1){
                     switch(data[0]){
                        case 0:
                           OnDisconnectedClient?.Invoke(_dataClient);
                           DataClients.Remove(_dataClient);
                        break;
                        case 1:
                           _dataClient.Ping = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _dataClient.Time - 1000;
                           _dataClient.Time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
                        break;
                     }
                  }
               }

               if(data.Length > 1){
                  var _data = Resources.ByteToReceive(data);
                  string _text = Encoding.UTF8.GetString(_data.Item1);
                  if(_text.StartsWith("<RSAKeyValue>") && _text.EndsWith("</RSAKeyValue>")){
                     _dataClient = new DataClient() {IP = _ip, Time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond), PublicKeyXML = _text};
                     DataClients.Add(_dataClient);
                     OnConnectedClient?.Invoke(_dataClient);
                     SendEncryption(_dataClient);
                  }else{
                     OnReceivedNewDataClient?.Invoke(_data.Item1, _data.Item2, _dataClient);
                  }
               }
            }catch(Exception ex){
               Resources.AddLogError(ex);
            }
            manualResetEvent.WaitOne();
         }
      }
      static void SendEncryption(DataClient _dataClient){
         try{
            if(Status == ServerStatusConnection.Running){
               string _text = Resources.PublicKeyXML;
               byte[] buffer  = Resources.ByteToSend(Encoding.UTF8.GetBytes(_text), _text.GetType(), false);
               MyServer.Send(buffer, buffer.Length, _dataClient.IP);
            }
         }catch{}
      }
      static void CheckOnline(){
         while(true){
            for(int i = 0; i < DataClients.Count; i++){
               if(DataClients.ElementAt(i).Time + 3000 < (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)){
                  OnDisconnectedClient?.Invoke(DataClients.ElementAt(i));
                  DataClients.RemoveAt(i);
               }
               manualResetEvent.WaitOne();
            }
            Thread.Sleep(1000);
         }
      }
      static void ChangeStatus(ServerStatusConnection _status){
         if(Status != _status){
            Status = _status;
            OnServerStatusConnection?.Invoke(_status);
         }
      }
   }
}