// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nethostfire {
   public class Server {
      static UdpClient MyServer;
      static IPEndPoint Host;
      static int PacketCount, PackTmp, TimeTmp, waitConnectionHold = 1000;
      static float PacketsReceived, PacketsSent;
      static ManualResetEvent manualResetEvent = new ManualResetEvent(true);
      static List<DataClient> DataClients = new List<DataClient>();
      static Dictionary<DataClient, HoldConnectionServer> ListHoldConnection = new Dictionary<DataClient, HoldConnectionServer>();
      static Thread CheckOnlineThread = new Thread(CheckOnline), ServerReceiveUDPThread = new Thread(ServerReceiveUDP);
      /// <summary>
      /// O evento é chamado quando bytes é recebido de um Client.
      /// </summary>
      public static Action<byte[], int, DataClient> OnReceivedNewDataClient;
      /// <summary>
      /// O evento é chamado quando o status do servidor muda.
      /// </summary>
      public static Action<ServerStatusConnection> OnServerStatusConnection;
      /// <summary>
      /// O evento é chamado quando um Client é conectado no servidor.
      /// </summary>
      public static Action<DataClient> OnConnectedClient;
      /// <summary>
      /// O evento é chamado quando um Client se desconecta do servidor.
      /// </summary>
      public static Action<DataClient> OnDisconnectedClient;
      /// <summary>
      /// Estado atual do Servidor.
      /// </summary>
      public static ServerStatusConnection Status {get;set;} = ServerStatusConnection.Stopped;
      /// <summary>
      /// Tempo de bloqueio para evitar duplos bytes recebidos do mesmo hashcode durante um pacote com o HoldConnection ligado. O valor padrão é 1000 (ms).
      /// </summary>
      public static int WaitConnectionHold {get {return waitConnectionHold;} set{waitConnectionHold = value;}}
      /// <summary>
      /// Número total de Clients conectado.
      /// </summary>
      public static int ClientsCount {get {return DataClients.Count;}}
      /// <summary>
      /// Quantidade de pacotes recebido por segundo (pps).
      /// </summary>
      public static string PacketsPerSeconds {get {return PackTmp +"pps";}}
      /// <summary>
      /// Tamanho total de pacotes recebido.
      /// </summary>
      public static string PacketsSizeReceived {get {return Resources.BytesToString(PacketsReceived);}}
      /// <summary>
      /// Tamanho total de pacotes enviado.
      /// </summary>
      public static string PacketsSizeSent {get {return Resources.BytesToString(PacketsSent);}}
      /// <summary>
      /// Inicia o servidor com um IP e Porta especifico.
      /// </summary>
      public static void Start(IPEndPoint _host = null){
         if(Status == ServerStatusConnection.Stopped || Status == ServerStatusConnection.Restarting || Status == ServerStatusConnection.FailedInitialize)
            ChangeStatus(ServerStatusConnection.Initializing);
         try{
            if(MyServer is null && _host is not null){
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
         }catch{
            if(_host is not null)
               Host = _host;
            ChangeStatus(ServerStatusConnection.FailedInitialize);
         }
         if(Status == ServerStatusConnection.Initializing)
            ChangeStatus(ServerStatusConnection.Running);
      }
      /// <summary>
      /// Pará o servidor. (Todos os Clients serão desconectados)
      /// </summary>
      public static void Stop(){
         if(Status == ServerStatusConnection.Running || Status == ServerStatusConnection.Restarting){
            if(Status == ServerStatusConnection.Running)
               ChangeStatus(ServerStatusConnection.Stopping);
            manualResetEvent.Reset();
            Thread.Sleep(3000);
            ListHoldConnection.Clear();
            DataClients.Clear();
            Resources.GenerateKeyRSA();
            if(Status == ServerStatusConnection.Stopping)
               ChangeStatus(ServerStatusConnection.Stopped);
         }
      }
      /// <summary>
      /// Reinicia o servidor. (Todos os Clients serão desconectados)
      /// </summary>
      public static void Restart(){
         ChangeStatus(ServerStatusConnection.Restarting);
         Stop();
         if(Host != null)
            Start(Host);
      }
      /// <summary>
      ///  Envie bytes para um Client especifico.
      /// </summary>
      public static void SendBytes(byte[] _byte, int _hashCode, DataClient _dataClient, bool _holdConnection = false){
         if(Status == ServerStatusConnection.Running){
            if(_holdConnection){
               if(!ListHoldConnection.ContainsKey(_dataClient))
                  ListHoldConnection.Add(_dataClient, new HoldConnectionServer{Bytes = new List<byte[]>{_byte}, Time = new List<long>{0}, HashCode = new List<int>{_hashCode}});
               else{
                  ListHoldConnection[_dataClient].Time.Add(0);
                  ListHoldConnection[_dataClient].Bytes.Add(_byte);
                  ListHoldConnection[_dataClient].HashCode.Add(_hashCode);
               }
            }
            Resources.Send(MyServer, _byte, _hashCode, _holdConnection, _dataClient);
            PacketsSent += _byte.Length;
         }
      }
      /// <summary>
      ///  Envie bytes para um grupo de Clients especifico.
      /// </summary>
      public static void SendBytesGroup(byte[] _byte, int _hashCode, List<DataClient> _dataClients, bool _holdConnection = false){
         Parallel.ForEach(_dataClients, _dataClient => {
            if(Status == ServerStatusConnection.Running){
               if(_holdConnection){
                  if(!ListHoldConnection.ContainsKey(_dataClient))
                     ListHoldConnection.Add(_dataClient, new HoldConnectionServer{Bytes = new List<byte[]>{_byte}, Time = new List<long>{0}, HashCode = new List<int>{_hashCode}});
                  else{
                     ListHoldConnection[_dataClient].Time.Add(0);
                     ListHoldConnection[_dataClient].Bytes.Add(_byte);
                     ListHoldConnection[_dataClient].HashCode.Add(_hashCode);
                  }
               }
               Resources.Send(MyServer, _byte, _hashCode, _holdConnection, _dataClient);
               PacketsSent += _byte.Length;
            }
         });
      }
      /// <summary>
      ///  Envie bytes para todos os Client conectado.
      /// </summary>
      public static void SendBytesAll(byte[] _byte, int _hashCode, bool _holdConnection = false){
         Parallel.ForEach(DataClients, _dataClient => {
            if(Status == ServerStatusConnection.Running){
               if(_holdConnection){
                  if(!ListHoldConnection.ContainsKey(_dataClient))
                     ListHoldConnection.Add(_dataClient, new HoldConnectionServer{Bytes = new List<byte[]>{_byte}, Time = new List<long>{0}, HashCode = new List<int>{_hashCode}});
                  else{
                     ListHoldConnection[_dataClient].Time.Add(0);
                     ListHoldConnection[_dataClient].Bytes.Add(_byte);
                     ListHoldConnection[_dataClient].HashCode.Add(_hashCode);
                  }
               }
               Resources.Send(MyServer, _byte, _hashCode, _holdConnection, _dataClient);
               PacketsSent += _byte.Length;
            }
         });
      }
      /// <summary>
      /// Deconecta um Client especifico do servidor.
      /// </summary>
      public static void DisconnectClient(DataClient _dataClient){
         try{
            if(Status == ServerStatusConnection.Running){
               Resources.SendPing(MyServer, new byte[]{0},_dataClient);
            }
         }catch{}
      }
      /// <summary>
      /// Deconecta um grupo de Clients do servidor.
      /// </summary>
      public static void DisconnectClientGroup(List<DataClient> _DataClients){
         Parallel.ForEach(_DataClients, _dataClient => {
            try{
               if(Status == ServerStatusConnection.Running){
                  Resources.SendPing(MyServer, new byte[]{0}, _dataClient);
               }
            }catch{}
         });
      }
      /// <summary>
      /// Deconecta todos os Clients conectado do servidor.
      /// </summary>
      public static void DisconnectClientAll(){
         Parallel.ForEach(DataClients, _dataClient => {
            try{
               if(Status == ServerStatusConnection.Running){
                  Resources.SendPing(MyServer, new byte[]{0},_dataClient);
               }
            }catch{}
         });
         Thread.Sleep(3000);
      }

      static IPEndPoint _ip = new IPEndPoint(IPAddress.Any, 0);
      static void ServerReceiveUDP(){
         if(MyServer != null)
         while(true){
            byte[] data = null;
            try{
               data = MyServer.Receive(ref _ip);
            }catch{}
            
            if(data != null){
               Parallel.Invoke(()=>{
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
                              if(ListHoldConnection.ContainsKey(_dataClient))
                                 ListHoldConnection.Remove(_dataClient);
                              OnDisconnectedClient?.Invoke(_dataClient);
                              DataClients.Remove(_dataClient);
                           break;
                           case 1:
                              _dataClient.Ping = ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _dataClient.Time - 1000).ToString() + "ms";
                              _dataClient.Time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
                              Resources.SendPing(MyServer, new byte[]{1},_dataClient);
                           break;
                        }
                     }
                  }

                  if(data.Length > 1 && Status == ServerStatusConnection.Running){
                     var _data = Resources.ByteToReceive(data, MyServer, _dataClient);
                     if(_data.Item3 && ListHoldConnection.ContainsKey(_dataClient) && ListHoldConnection[_dataClient].HashCode.Contains(_data.Item2)){
                        var index = ListHoldConnection[_dataClient].HashCode.IndexOf(_data.Item2);
                        ListHoldConnection[_dataClient].Bytes.RemoveAt(index);
                        ListHoldConnection[_dataClient].Time.RemoveAt(index);
                        ListHoldConnection[_dataClient].HashCode.RemoveAt(index);
                     }
                     else
                     if(_dataClient.PublicKeyXML == ""){
                        string _text = Encoding.UTF8.GetString(_data.Item1);
                        if(_text.StartsWith("<RSAKeyValue>") && _text.EndsWith("</RSAKeyValue>")){
                           _dataClient = new DataClient() {IP = _ip, Time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond), PublicKeyXML = _text};
                           DataClients.Add(_dataClient);
                           OnConnectedClient?.Invoke(_dataClient);
                           byte[] _byte  = Encoding.UTF8.GetBytes(Resources.PublicKeyXML);
                           Resources.Send(MyServer, _byte, _byte.GetHashCode(), false, _dataClient);
                        }
                     }else{
                        PacketsReceived += _data.Item1.Length;
                        if(ListHoldConnection.ContainsKey(_dataClient) && ListHoldConnection[_dataClient].HashCode.Contains(_data.Item2)){
                           var index = ListHoldConnection[_dataClient].HashCode.IndexOf(_data.Item2);
                           if(ListHoldConnection[_dataClient].Time[index] == 0){
                              ListHoldConnection[_dataClient].Time[index] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + waitConnectionHold;
                              OnReceivedNewDataClient?.Invoke(_data.Item1, _data.Item2, _dataClient);
                           }else{
                              if(ListHoldConnection[_dataClient].Time[index] < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond){
                                 ListHoldConnection.Remove(_dataClient);
                              }
                           }
                        }else{
                           OnReceivedNewDataClient?.Invoke(_data.Item1, _data.Item2, _dataClient);
                        }
                     }
                  }
               });
            }
            manualResetEvent.WaitOne();
         }
      }

      static void CheckOnline(){
         while(true){
            Parallel.ForEach(DataClients, _dataClient =>{
               try{
                  if(_dataClient.Time + 3000 < (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)){
                     if(ListHoldConnection.ContainsKey(_dataClient))
                        ListHoldConnection.Remove(_dataClient);
                     OnDisconnectedClient?.Invoke(_dataClient);
                     DataClients.Remove(_dataClient);
                  }
               }catch{}
                if(Status == ServerStatusConnection.Running){
                    try{
                        foreach(var item in ListHoldConnection.ToArray()){
                           for(int i = 0; i < item.Value.HashCode.Count; i++){
                              Resources.Send(MyServer, item.Value.Bytes[i], item.Value.HashCode[i], true);
                           }
                        };
                    }catch{}
                }
               manualResetEvent.WaitOne();
            });
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