// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Nethostfire {
    /// <summary>
    /// The DataClient class is used to store data from a client on the Server. It is with this class that the server uses to define a client. The DataClients can be obtained with the following server events Server.OnReceivedNewDataClient, Client.OnReceivedNewDataServer, Server.OnConnectedClient and Server.OnDisconnectedClient
    /// </summary>
    public class DataClient{
        /// <summary>
        /// IP Address
        /// </summary>
        public IPEndPoint IP;
        /// <summary>
        /// Packets per second
        /// </summary>
        public int PPS;
        /// <summary>
        /// Ping (ms)
        /// </summary>
        public int Ping;
        /// <summary>
        /// Last time updated by the server.
        /// </summary>
        public int Time;
        /// <summary>
        /// Last time received packet.
        /// </summary>
        public int TimeLastPacket;
        /// <summary>
        /// RSA key
        /// </summary>
        public string PublicKeyRSA = null;
        /// <summary>
        /// Private AES key
        /// </summary>
        public byte[] PrivateKeyAES = null;
        public readonly ConcurrentDictionary<int, HoldConnection> ListHoldConnection = new();
        public readonly ConcurrentDictionary<int, HoldConnection> ListHoldConnectionQueue = new();
    }
    
    /// <summary>
    /// The TypeHoldConnection is a feature to guarantee the sending of udp packets even with packet losses.
    /// </summary>
    public enum TypeHoldConnection {
        None = 0,
        /// <summary>
        /// With NotEnqueue, bytes are sent to their destination without packet loss, shipments will not be queued to improve performance. (PS: it is possible that the same byte is received twice, increase the value of ReceiveAndSendTimeOut if this happens)
        /// </summary>
        NotEnqueue = 1,
        /// <summary>
        /// With Enqueue, bytes are sent to their destination without packet loss, shipments will be sent in a queue, this feature is not recommended to be used for high demand for shipments, each package can vary between 1ms and 1000ms. (PS: it is possible that the same byte is received twice, increase the value of ReceiveAndSendTimeOut if this happens)
        /// </summary>
        Enqueue = 2,
    }

    /// <summary>
    /// The ServerStatusConnection is used to define server states. The ServerStatusConnection can be obtained by the Server.Status variable or with the event OnServerStatusConnection
    /// </summary>
    public enum ServerStatusConnection{
        Stopped = 0,
        Stopping = 1,
        Running = 2,
        Initializing = 3,
        Restarting = 4,
    }
    public enum TypeContent{
        Background = 0,
        Foreground = 1,
    }
    enum TypeUDP{
        Server = 0,
        Client = 1,
    }
    /// <summary>
    /// The TypeShipping is used to define the type of encryption of the bytes when being sent.
    /// </summary>
    public enum TypeShipping {
        /// <summary>
        /// The bytes will not be modified when sent.
        /// </summary>
        None = 0,
        /// <summary>
        /// The bytes will be sent encrypted in AES and then automatically decrypted after reaching their destination.
        /// </summary>
        AES = 1,
        /// <summary>
        /// The bytes will be sent encrypted in RSA and then automatically decrypted after reaching their destination.
        /// </summary>
        RSA = 2,
        /// <summary>
        /// The bytes will be sent encrypted in Base64 and then automatically decrypted after reaching their destination.
        /// </summary>
        Base64 = 3,
        /// <summary>
        /// The bytes will be sent compress and then automatically decompress after reaching their destination.
        /// </summary>
        Compress = 4,
        /// <summary>
        /// The bytes will be sent encrypted in Base64 and if your destination is a client, they will be automatically decrypted when you reach your destination, but if the destination is a server the bytes will not be decrypted.
        /// </summary>
        OnlyBase64 = 5,
        /// <summary>
        /// The bytes will be sent compress and if your destination is a client, they will be automatically decompress when you reach your destination, but if the destination is a server the bytes will not be decompress.
        /// </summary>
        OnlyCompress = 6,
    }
    /// <summary>
    /// The ClientStatusConnection is used to define client states. The ClientStatusConnection can be obtained by the Client.Status variable or with the event Client.OnClientStatusConnection
    /// </summary>
    public enum ClientStatusConnection{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        ConnectionFail = 4,
        IpBlocked = 5,
        MaxClientExceeded = 6,
    }

    class LimitMaxPPS {
        public bool NotLimited {get{
            if(Environment.TickCount >= Timer + (1000f / PPS)){
                Timer = Environment.TickCount;
                return true;
            }else
                return false;
            }
        }
        public int PPS;
        public float Timer;
    }
    
    public struct HoldConnection{
        public int GroupID;
        public byte[] Bytes {get;set;}
        public TypeShipping TypeShipping {get;set;}
        public TypeContent TypeContent {get;set;}
        public TypeHoldConnection TypeHoldConnection {get;set;}
    }

    class Utility {
        public static string PublicKeyRSAClient, PrivateKeyRSAClient, PublicKeyRSAServer, PrivateKeyRSAServer;
        public static byte[] PrivateKeyAESClient, PrivateKeyAESServer;
        public static int receiveAndSendTimeOut = 1000;
        public static bool RunningInUnity = false, ShowDebugConsole = true, UnityBatchMode = false, UnityEditorMode = false;
        public static ConcurrentQueue<Action> ListRunOnMainThread = new();
        public static ConcurrentDictionary<int, HoldConnection> listHoldConnectionClient = new();
        public static ConcurrentDictionary<int, HoldConnection> listHoldConnectionQueueClient = new();
        public static List<int> listIndex = new();
        public static int IndexShipping = 1;
        static Aes AES;
        public static string GetVersion {get {return UnityEngine.Application.version;}}
        public static Process Process = Process.GetCurrentProcess();
        public static bool Quitting = false;

        public static void RunOnMainThread(Action _action){
            if(RunningInUnity && Quitting == false)
                ListRunOnMainThread.Enqueue(_action);
            else
                _action?.Invoke();
        }

        public static void ThisMainThread() {
            while(ListRunOnMainThread.TryDequeue(out var _action))
                if(UnityBatchMode){
                    Parallel.Invoke(() =>{
                        _action?.Invoke();
                    });
                }else
                    _action?.Invoke();
        }

        public static string BytesToString(float PacketsReceived){
            if(PacketsReceived > 1024000000)
            return (PacketsReceived / 1024000000).ToString("0.00") + "GB";
            if(PacketsReceived > 1024000)
            return (PacketsReceived / 1024000).ToString("0.00") + "MB";
            if(PacketsReceived > 1024)
            return (PacketsReceived / 1024).ToString("0.00") + "KB";
            if(PacketsReceived < 1024)
            return PacketsReceived + "Bytes";
            return "";
        }

        public static void StartUnity(){
            try{
                LoadUnity();
                RunningInUnity = true;
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    if(UnityBatchMode || !RunningInUnity)
                        Process.PriorityClass = ProcessPriorityClass.High;
            }catch{}
        }

        public static void GenerateKey(TypeUDP typeUDP,int SymmetricSize){
            string publicKeyRSA, privateKeyRSA;
            byte[] privateKeyAES;
            AES ??= Aes.Create();
            int value = (SymmetricSize - 6) * 8 + 384;
            if(value >= 464 && value <= 4096){
                using(var RSA = new RSACryptoServiceProvider(value)){
                    privateKeyRSA = RSA.ToXmlString(true);
                    publicKeyRSA = RSA.ToXmlString(false);
                }
                privateKeyAES = GetHashMD5(Encoding.ASCII.GetBytes(privateKeyRSA));
            }else
                throw new Exception(ShowLog("RSA SymmetricSize cannot be less than " + ((464 - 384) / 8 + 6) + " or greater than " + ((4096 - 384) / 8 + 6)));
           
            switch(typeUDP){
                case TypeUDP.Server:
                    PrivateKeyRSAServer = privateKeyRSA;
                    PublicKeyRSAServer = publicKeyRSA;
                    PrivateKeyAESServer = privateKeyAES;
                break;
                case TypeUDP.Client:
                    PrivateKeyRSAClient = privateKeyRSA;
                    PublicKeyRSAClient = publicKeyRSA;
                    PrivateKeyAESClient = privateKeyAES;
                break;
            }
        }

        public static (byte[], int, TypeContent, TypeShipping, int) ByteToReceive(byte[] _byte, UdpClient _udpClient, DataClient _dataClient = null){
            TypeShipping _TypeShipping;
            TypeContent _typeContent;
            byte[] data;
            byte[] type;
            int _groupID;
            int _indexID;

            try{
                if(_byte[0] == 3 && _byte.Length > 1){                              // confirm respond
                    type = new byte[_byte[1]];
                    type = _byte.Skip(2).ToArray().Take(_byte[1]).ToArray();
                    _indexID = BitConverter.ToInt16(type, 0);
                        
                    if(_dataClient != null)
                        if(_dataClient.ListHoldConnectionQueue.Count > 0)
                        if(_dataClient.ListHoldConnectionQueue.ElementAt(0).Key == _indexID)
                            if(_dataClient.ListHoldConnectionQueue.TryRemove(_indexID, out _))
                                if(_dataClient.ListHoldConnectionQueue.Count > 0){
                                    var _lhcq = _dataClient.ListHoldConnectionQueue.ElementAt(0).Value;
                                    UDP.Server.Send(_lhcq.Bytes, _lhcq.GroupID, _dataClient, _lhcq.TypeShipping, TypeHoldConnection.NotEnqueue);
                            }

                    return (new byte[]{}, 0, TypeContent.Background, TypeShipping.None, _indexID);
                }

                type = new byte[_byte[3]];
                type = _byte.Skip(5).Take(_byte[3]).ToArray();
                _groupID = BitConverter.ToInt16(type, 0);                       // getting value of groupid

                type = new byte[_byte[4]];
                type = _byte.Skip(5 + _byte[3]).Take(_byte[4]).ToArray();
                _indexID = BitConverter.ToInt32(type, 0);                       // getting value of indexid
                
                data = new byte[_byte.Length - _byte[3] - _byte[4] - 5];
                _byte.Skip(5 + _byte[3] +  _byte[4]).ToArray().CopyTo(data,0);

                _TypeShipping = (TypeShipping)_byte[2];
                _typeContent = (TypeContent)_byte[1];
            }catch{
                return (new byte[]{}, 0, TypeContent.Background, TypeShipping.None, 0);
            }

            if(_typeContent == TypeContent.Background)
            switch(_TypeShipping){
                case TypeShipping.RSA:
                    data = Decompress(data);
                break;
                case TypeShipping.AES:
                    data = DecryptRSA(data, _dataClient != null ? PrivateKeyRSAServer : PrivateKeyRSAClient);
                break;
            }


            if(_typeContent == TypeContent.Foreground)
            switch(_TypeShipping){
                case TypeShipping.AES:
                    data = DecryptAES(data, _dataClient != null ? _dataClient.PrivateKeyAES : PrivateKeyAESClient);
                break;
                case TypeShipping.RSA:
                    data = DecryptRSA(data, _dataClient != null ? PrivateKeyRSAServer : PrivateKeyRSAClient);
                break;
                case TypeShipping.Base64:
                    data = DecryptBase64(Encoding.ASCII.GetString(data));
                break;
                case TypeShipping.Compress:
                    data = Decompress(data);
                break;
                case TypeShipping.OnlyBase64:
                    if(_dataClient == null)
                        data = DecryptBase64(Encoding.ASCII.GetString(data));
                break;
                case TypeShipping.OnlyCompress:
                    if(_dataClient == null)
                        data = Decompress(data);
                break;
            }

            if(_TypeShipping != TypeShipping.None && _byte.Length == 0)
                return (null, 0, TypeContent.Background, TypeShipping.None, 0);

            try{
                if(_byte[0] == 1 || _byte[0] == 2){                         // return auto
                    byte[] data2 = new byte[_byte[4] + 2];
                    data2[0] = 3;                                           // Hold Connection respond
                    data2[1] = _byte[4];                                    // The size of indexID
                    type.CopyTo(data2, 2);                                  // IndexID
                    SendPing(_udpClient, data2, _dataClient);
                }
                if(!listIndex.Contains(_indexID)){
                    listIndex.Add(_indexID);
                    return (data.Length > 1 ? data : new byte[]{}, _groupID, _typeContent, _TypeShipping, _indexID);
                }else{
                    return (new byte[]{}, 0, TypeContent.Background, TypeShipping.None, 0);
                }
            }catch{
                return (new byte[]{}, 0, TypeContent.Background, TypeShipping.None, 0);
            }
        }
        
        public static byte[] ByteToSend(byte[] _byte, int _groupID, TypeShipping _TypeShipping, TypeHoldConnection _typeHoldConnection, TypeContent _typeContent, int _indexID, DataClient _dataClient = null){
            if(_typeContent == TypeContent.Background)
            switch(_TypeShipping){
                case TypeShipping.RSA:
                    _byte = Compress(_byte);
                break;
                case TypeShipping.AES:
                    _byte = EncryptRSA(_byte, _dataClient != null ? _dataClient.PublicKeyRSA : UDP.Client.PublicKeyRSA);
                break;
            }

            if(_typeContent == TypeContent.Foreground)
            switch(_TypeShipping){
                case TypeShipping.AES:
                    _byte = EncryptAES(_byte, _dataClient != null ? _dataClient.PrivateKeyAES : PrivateKeyAESClient);
                break;
                case TypeShipping.RSA:
                    _byte = EncryptRSA(_byte, _dataClient != null ? _dataClient.PublicKeyRSA : UDP.Client.PublicKeyRSA);
                break;
                case TypeShipping.Base64:
                    _byte = EncryptBase64(_byte) == "" ? new byte[]{} : System.Text.Encoding.ASCII.GetBytes(EncryptBase64(_byte));
                break;
                case TypeShipping.Compress:
                    _byte = Compress(_byte);
                break;
                case TypeShipping.OnlyBase64:
                    if(_dataClient == null)
                    _byte = EncryptBase64(_byte) == "" ? new byte[]{} : System.Text.Encoding.ASCII.GetBytes(EncryptBase64(_byte));
                break;
                case TypeShipping.OnlyCompress:
                    if(_dataClient == null)
                        _byte = Compress(_byte);
                break;
            }

             if(_TypeShipping != TypeShipping.None && _byte.Length == 0)
                return new byte[]{};

            try{
                byte[] groupID = BitConverter.GetBytes(_groupID);
                byte[] indexID = BitConverter.GetBytes(_indexID);
                byte[] data = new byte[_byte.Length + indexID.Length + groupID.Length + 5]; //5 é a quantidade de dados

                //data
                data[0] = (byte)_typeHoldConnection;                // If is HoldConnection. 0 = disable, 1 = not enqueued, 2 = enqueued
                data[1] = (byte)_typeContent;                       // O tipo de conteúdo
                data[2] = (byte)_TypeShipping;                      // O tipo de criptografia
                data[3] = (byte)groupID.Length;                     // O tamanho do groupID
                data[4] = (byte)indexID.Length;                     // O tamanho do index

                groupID.CopyTo(data, 5);                                    // GroupID
                indexID.CopyTo(data, 5 + groupID.Length);                   // indexID
                _byte.CopyTo(data, 5 + indexID.Length + groupID.Length);    // bytes
                return data;
            }
            catch{
                return new byte[]{};
            }
        }

        public static bool Send(UdpClient _udpClient, byte[] _byte, int _groupID, TypeShipping _typeShipping, TypeHoldConnection _typeHoldConnection, TypeContent _typeContent, int _indexID, DataClient _dataClient = null){
            byte[] _buffer = ByteToSend(_byte, _groupID, _typeShipping, _typeHoldConnection, _typeContent, _indexID, _dataClient);
            try{
                if(_buffer.Length != 0){
                    if(_dataClient == null){
                        if(_typeHoldConnection == TypeHoldConnection.NotEnqueue)
                            listHoldConnectionClient.TryAdd(_indexID, new HoldConnection{GroupID = _groupID, Bytes = _byte, TypeShipping = _typeShipping, TypeContent = UDP.Client.Status == ClientStatusConnection.Connected ? TypeContent.Foreground : TypeContent.Background, TypeHoldConnection = _typeHoldConnection});
                        
                        if(_typeHoldConnection == TypeHoldConnection.Enqueue){
                           if(listHoldConnectionQueueClient.Count > 0){
                                if(listHoldConnectionQueueClient.ElementAt(0).Key == _indexID)
                                    _udpClient.Send(_buffer, _buffer.Length);
                           }else{
                                listHoldConnectionClient.TryAdd(_indexID, new HoldConnection{GroupID = _groupID, Bytes = _byte, TypeShipping = _typeShipping, TypeContent = UDP.Client.Status == ClientStatusConnection.Connected ? TypeContent.Foreground : TypeContent.Background, TypeHoldConnection = TypeHoldConnection.NotEnqueue});
                                _udpClient.Send(_buffer, _buffer.Length);
                           }
                        }else
                            _udpClient.Send(_buffer, _buffer.Length);
                    }else{   
                        if(_typeHoldConnection == TypeHoldConnection.NotEnqueue)
                            _dataClient.ListHoldConnection.TryAdd(_indexID, new HoldConnection(){GroupID = _groupID, Bytes = _byte, TypeShipping = _typeShipping, TypeContent = _dataClient.PrivateKeyAES != null ? TypeContent.Foreground : TypeContent.Background, TypeHoldConnection = _typeHoldConnection});

                        if(_typeHoldConnection == TypeHoldConnection.Enqueue){
                            if(_dataClient.ListHoldConnectionQueue.Count > 0){
                                if(_dataClient.ListHoldConnectionQueue.ElementAt(0).Key == _indexID)
                                    _udpClient.Send(_buffer, _buffer.Length, _dataClient.IP);
                            }else{
                                _dataClient.ListHoldConnection.TryAdd(_indexID, new HoldConnection(){GroupID = _groupID, Bytes = _byte, TypeShipping = _typeShipping, TypeContent = _dataClient.PrivateKeyAES != null ? TypeContent.Foreground : TypeContent.Background, TypeHoldConnection = TypeHoldConnection.NotEnqueue});
                                _udpClient.Send(_buffer, _buffer.Length, _dataClient.IP);
                            }
                        }else
                            _udpClient.Send(_buffer, _buffer.Length, _dataClient.IP);
                    }
                    return true;
                }else
                    return false;
            }
            catch{
                return false;
            }
        }

        /// <summary>
        /// Envia os bytes para Cliente/Server especifico sem as informações adicionais.
        /// </summary>
        public static bool SendPing(UdpClient _udpClient, byte[] _byte, DataClient _dataClient = null){
            try{
                if(_dataClient == null)
                    _udpClient.Send(_byte, _byte.Length);
                else
                    _udpClient.Send(_byte, _byte.Length, _dataClient.IP);
                return true;
            }catch{
                return false;
            }
        }
        private static byte[] EncryptRSA(byte[] _byte, string _publicKeyRSA){
            using var RSA = new RSACryptoServiceProvider();
            try
            {
                RSA.FromXmlString(_publicKeyRSA);
                return RSA.Encrypt(_byte, true);
            }
            catch
            {
                var b = ((RSA.KeySize - 384) / 8) + 6;
                if (b < _byte.Length)
                    ShowLog("The key size defined in KeySizeBytesRSA, can only encrypt at most " + b + " bytes.");
                return new byte[]{};
            }
        }
        private static byte[] DecryptRSA(byte[] _byte, string _privateKeyRSA){
            using var RSA = new RSACryptoServiceProvider();
            try
            {
                RSA.FromXmlString(_privateKeyRSA);
                return RSA.Decrypt(_byte, true);
            }
            catch
            {
                var b = ((RSA.KeySize - 384) / 8) + 6;
                if (b < _byte.Length)
                    ShowLog("The key size defined in KeySizeBytesRSA, can only decrypt at most " + b + " bytes.");
                return new byte[]{};
            }
        }
        private static byte[] EncryptAES(byte[] _byte, byte[] _privateKeyAES){
            try{
                using var encryptor = AES.CreateEncryptor(_privateKeyAES, _privateKeyAES);
                return encryptor.TransformFinalBlock(_byte, 0, _byte.Length);
            }
            catch{
                return new byte[]{};
            }
        }
        private static byte[] DecryptAES(byte[] _byte, byte[] _privateKeyAES){
            try{
                using var encryptor = AES.CreateDecryptor(_privateKeyAES, _privateKeyAES);
                return encryptor.TransformFinalBlock(_byte, 0, _byte.Length);
            }
            catch{
                return new byte[]{};
            }
        }
        private static string EncryptBase64(byte[] _byte){
            try{
                return Convert.ToBase64String(_byte);
            }catch{
                return "";
            }
        }
        private static byte[] DecryptBase64(string _text){
            try{
                return Convert.FromBase64String(_text);
            }catch{
                return new byte[]{};
            }
        }
        private static byte[] Compress(byte[] _byte){
            try{
                MemoryStream output = new();
                using (DeflateStream dstream = new(output, CompressionMode.Compress)){
                    dstream.Write(_byte, 0, _byte.Length);
                }
                return output.ToArray();
            }catch{
                return new byte[]{};
            }
        }
        private static byte[] Decompress(byte[] data){
            try{
                MemoryStream input = new(data);
                MemoryStream output = new();
                using (DeflateStream dstream = new(input, CompressionMode.Decompress)){
                    dstream.CopyTo(output);
                }
                return output.ToArray();
            }catch{
                return new byte[]{};
            }
        }
        private static byte[] GetHashMD5(byte[] _byte){
            try{
                MD5 md5 = MD5.Create();
                return md5.ComputeHash(_byte);
            }catch{
                return new byte[]{};
            }
        }
        public static string ShowLog(string Message){
            if(ShowDebugConsole)
                if(RunningInUnity && !UnityBatchMode)
                    ShowUnityLog(Message);
                else{
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("[NETHOSTFIRE] ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(DateTime.Now + " ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(Message);
                }
            return Message;
        }

        //================= Funções com dll da Unity =================
        static void ShowUnityLog(string Message){
            UnityEngine.Debug.unityLogger.logEnabled = ShowDebugConsole; 
            UnityEngine.Debug.Log("<color=red>[NETHOSTFIRE]</color> " + Message);
        }

        public static void LoadUnity(){
            UnityBatchMode = UnityEngine.Application.isBatchMode;
            UnityEditorMode = UnityEngine.Application.isEditor;
            if(!UnityEngine.GameObject.Find("Nethostfire")){
                if(UnityBatchMode)
                    Console.Clear();
                UnityEngine.GameObject runThreadUnity = new("Nethostfire");
                runThreadUnity.AddComponent<NethostfireService>();
            }
        }
    }
}