// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Nethostfire {
    /// <summary>
    /// The DataClient class is used to store data from a client on the server. It is with this class that the server uses to define a client. The DataClients can be obtained with the following server events Server.OnReceivedNewDataClient, Client.OnReceivedNewDataServer, Server.OnConnectedClient and Server.OnDisconnectedClient
    /// </summary>
    public class DataClient{
        public IPEndPoint IP;
        public int PPS;
        public int Ping;
        public int Time;
        public int TimeLastPacket;
        public string PublicKeyRSA = null;
        public byte[] PrivateKeyAES = null;
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
    /// <summary>
    /// The TypeEncrypt is used to define the type of encryption of the bytes when being sent.
    /// </summary>
    public enum TypeEncrypt{
        None = 0,
        AES = 1,
        RSA = 2,
        Base64 = 3,
        Compress = 4,
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

    class HoldConnectionServer{
        public List<int> GroupID {get;set;}
        public List<byte[]> Bytes {get;set;}
        public List<int> Time {get;set;}
    }
    
    class HoldConnectionClient{
        public byte[] Bytes {get;set;}
        public int Time {get;set;}
        public TypeEncrypt TypeEncrypt {get;set;}
        public TypeContent TypeContent {get;set;}
    }

    class Utility {
        public static string PrivateKeyRSA = "", PublicKeyRSA = "";
        public static byte[] PrivateKeyAES;
        public static bool RunningInUnity = false, ShowDebugConsole = true;
        public static readonly ConcurrentQueue<Action> ListRunOnMainThread = new ConcurrentQueue<Action>();
        static RSACryptoServiceProvider RSA;
        static Aes AES; 
        public static Stopwatch Timer = new Stopwatch();
        public static Process Process = Process.GetCurrentProcess();

        public static void RunOnMainThread(Action action){
            if(RunningInUnity)
                ListRunOnMainThread.Enqueue(action);
            else
                action?.Invoke();
        }

        public static void ThisMainThread() {
            while(ListRunOnMainThread.TryDequeue(out var action)) {
                action?.Invoke();
            }
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
            }catch{}
        }

        public static void GenerateKeyRSA(int SymmetricSize){
            AES = Aes.Create();
            int value = (SymmetricSize - 6) * 8 + 384;
            if(value >= 464 && value <= 4096){
                RSA = new RSACryptoServiceProvider(value);
                PrivateKeyRSA = RSA.ToXmlString(true);
                PublicKeyRSA = RSA.ToXmlString(false);
                PrivateKeyAES = GetHashMD5(System.Text.Encoding.ASCII.GetBytes(PrivateKeyRSA));
            }else
                throw new Exception(Utility.ShowLog("RSA SymmetricSize cannot be less than " + ((464 - 384) / 8 + 6) + " or greater than " + ((4096 - 384) / 8 + 6)));
        }

        public static (byte[], int, TypeContent, TypeEncrypt) ByteToReceive(byte[] _byte, UdpClient _udpClient, DataClient _dataClient = null){
            TypeEncrypt _typeEncrypt;
            TypeContent _typeContent;
            byte[] data;
            byte[] type;
            int hascode;
            try{
                if(_byte[0] == 2){
                    type = new byte[_byte[1]];
                    type = _byte.Skip(2).ToArray().Take(_byte[1]).ToArray();
                    return (null, BitConverter.ToInt32(type, 0), TypeContent.Background, TypeEncrypt.None);
                }

                type = new byte[_byte[3]];
                type = _byte.Skip(4).ToArray().Take(_byte[3]).ToArray();
                data = new byte[_byte.Length - type.Length - 4];
                _byte.Skip(4 + _byte[3]).ToArray().CopyTo(data,0);

                hascode = BitConverter.ToInt32(type, 0);
                _typeEncrypt = (TypeEncrypt)_byte[2];
                _typeContent = (TypeContent)_byte[1];
            }catch{
                return (null, -1, TypeContent.Foreground, TypeEncrypt.None);
            }

            if(_typeContent == TypeContent.Background)
                switch(_typeEncrypt){
                    case TypeEncrypt.RSA:
                        data = Decompress(data);
                    break;
                    case TypeEncrypt.AES:
                        data = DecryptRSA(data);
                    break;
                }

            if(_typeContent == TypeContent.Foreground)
                switch(_typeEncrypt){
                    case TypeEncrypt.AES:
                        data = DecryptAES(data, _dataClient);
                    break;
                    case TypeEncrypt.RSA:
                        data = DecryptRSA(data);
                    break;
                    case TypeEncrypt.Base64:
                        data = DecryptBase64(System.Text.Encoding.ASCII.GetString(data));
                    break;
                    case TypeEncrypt.Compress:
                        data = Decompress(data);
                    break;
                }

            try{
                if(_byte[0] == 1){
                    byte[] data2 = new byte[_byte[3] + 2];
                    data2[0] = 2;
                    data2[1] = _byte[3];
                    type.CopyTo(data2, 2);
                    SendPing(_udpClient, data2, _dataClient);
                }
                
                return (data, hascode, _typeContent, _typeEncrypt);
            }catch{
                return (null, -1, TypeContent.Foreground, TypeEncrypt.None);
            }
        }
        
        public static byte[] ByteToSend(byte[] _byte, int _groupID, TypeEncrypt _typeEncrypt, bool _holdConnection, TypeContent _typeContent, DataClient _dataClient = null){
            if(_typeContent == TypeContent.Background)
                switch(_typeEncrypt){
                    case TypeEncrypt.RSA:
                        _byte = Compress(_byte);
                    break;
                    case TypeEncrypt.AES:
                        _byte = EncryptRSA(_byte, _dataClient);
                    break;
                }

            if(_typeContent == TypeContent.Foreground)
            switch(_typeEncrypt){
                case TypeEncrypt.AES:
                    _byte = EncryptAES(_byte, _dataClient);
                break;
                case TypeEncrypt.RSA:
                    _byte = EncryptRSA(_byte, _dataClient);
                break;
                case TypeEncrypt.Base64:
                    _byte = System.Text.Encoding.ASCII.GetBytes(EncryptBase64(_byte));
                break;
                case TypeEncrypt.Compress:
                    _byte = Compress(_byte);
                break;
            }   
           
            try{
                byte[] hascode = BitConverter.GetBytes(_groupID);
                byte[] data = new byte[_byte.Length + hascode.Length + 4];

                data[0] = (byte)(_holdConnection ? 1 : 0);          // Se é um Hold Connection
                data[1] = (byte)_typeContent;                       // O tipo de conteúdo
                data[2] = (byte)_typeEncrypt;                       // O tipo de criptografia
                data[3] = (byte)hascode.Length;                     // O tamanho do hascode

                hascode.CopyTo(data, 4);
                _byte.CopyTo(data, 4 + hascode.Length);

                return data;
            }
            catch{
                return null;
            }
        }

        public static bool Send(UdpClient _udpClient, byte[] _byte, int _groupID, TypeEncrypt _typeEncrypt, bool _holdConnection, TypeContent _typeContent, DataClient _dataClient = null){
            byte[] buffer = ByteToSend(_byte, _groupID, _typeEncrypt, _holdConnection, _typeContent, _dataClient);
            try{
                if(buffer != null){
                    if(_dataClient == null)
                        _udpClient.Send(buffer, buffer.Length);
                    else
                        _udpClient.Send(buffer, buffer.Length, _dataClient.IP);
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
        private static byte[] EncryptRSA(byte[] _byte, DataClient _dataClient){
            try{
                RSA.FromXmlString(_dataClient != null ? _dataClient.PublicKeyRSA : Client.PublicKeyRSA);
                return RSA.Encrypt(_byte, true);
            }catch{
                var b = ((RSA.KeySize - 384) / 8) + 6;
                if(b < _byte.Length)
                    Utility.ShowLog("The key size defined in KeySizeBytesRSA, can only encrypt at most " + b + " bytes.");
                return new byte[]{};
            }
        }
        private static byte[] DecryptRSA(byte[] _byte){
            try{
                RSA.FromXmlString(PrivateKeyRSA);
                return RSA.Decrypt(_byte, true);
            }catch{
                var b = ((RSA.KeySize - 384) / 8) + 6;
                if(b < _byte.Length)
                    Utility.ShowLog("The key size defined in KeySizeBytesRSA, can only encrypt at most " + b + " bytes.");
                return new byte[]{};
            }
        }
        private static byte[] EncryptAES(byte[] _byte, DataClient _dataClient = null){
            try{
                var key = _dataClient != null ? _dataClient.PrivateKeyAES : PrivateKeyAES;
                using (var encryptor = AES.CreateEncryptor(key, key))
                return encryptor.TransformFinalBlock(_byte, 0, _byte.Length);
            }catch{
                return new byte[]{};
            }
        }
        private static byte[] DecryptAES(byte[] _byte, DataClient _dataClient = null){
            try{
                var key = _dataClient != null ? _dataClient.PrivateKeyAES : PrivateKeyAES;
                using (var encryptor = AES.CreateDecryptor(key, key))
                return encryptor.TransformFinalBlock(_byte, 0, _byte.Length);
            }catch{
                return new byte[]{};
            }
        }
        private static string EncryptBase64(byte[] _byte){
            return Convert.ToBase64String(_byte);
        }
        private static byte[] DecryptBase64(string _text){
            return Convert.FromBase64String(_text);
        }
        private static byte[] Compress(byte[] _byte){
            try{
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress)){
                    dstream.Write(_byte, 0, _byte.Length);
                }
                return output.ToArray();
            }catch{
                return new byte[]{};
            }
        }
        private static byte[] Decompress(byte[] data){
            try{
                MemoryStream input = new MemoryStream(data);
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress)){
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
                if(RunningInUnity)
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

        static void LoadUnity(){
            if(!UnityEngine.GameObject.Find("Nethostfire")){
                UnityEngine.GameObject runThreadUnity = new UnityEngine.GameObject("Nethostfire");
                runThreadUnity.AddComponent<ServiceNetwork>(); 
            }
        }
    }
}