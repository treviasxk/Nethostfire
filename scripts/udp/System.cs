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
    class DataClient{
        // Public key RSA to encrypt bytes
        public string? PublicKeyRSA;
        // Private key AES to encrypt bytes
        public byte[]? PrivateKeyAES;
        // Timer to check if client is connected
        public long LastTimer;
        // Timer to check packets interval
        public long MaxPPSTimer;
        // Ping
        public int Ping;
        // IndexID to send bytes
        public int IndexID;
        // List to check packets duplications
        public HashSet<int> ListIndex = new();
        // List shippiments without packet loss
        public readonly ConcurrentDictionary<int, byte[]> ListHoldConnection = new();
        // List shippiments without packet loss queued
        public readonly ConcurrentDictionary<int, byte[]> QueuingHoldConnection = new();
        // Limit max receive pps
        public int LimitMaxPPS;
        // Limit max receive pps for GroupID
        public readonly ConcurrentDictionary<int, int> LimitMaxPPSGroupID = new();
    }

    public enum ServerStatus{
        Stopped = 0,
        Stopping = 1,
        Running = 2,
        Initializing = 3,
        Restarting = 4,
    }

    public enum ClientStatus{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        ConnectionFail = 4,
        IpBlocked = 5,
        MaxClientExceeded = 6,
    }

    public enum MySQLStatus {
        Connected,
        Disconnected
    }


    /// <summary>
    /// The TypeHoldConnection is a feature to guarantee the sending of udp packets even with packet losses.
    /// </summary>
    public enum TypeShipping {
        // 0 = background
        // 1 = Respond
        None = 2,
        /// <summary>
        /// With WithoutPacketLoss, bytes are sent to their destination without packet loss, shipments will not be queued to improve performance.
        /// </summary>
        WithoutPacketLoss = 3,
        /// <summary>
        /// With WithoutPacketLossEnqueue, bytes are sent to their destination without packet loss, shipments will be sent in a queue, this feature is not recommended to be used for high demand for shipments, each package can vary between 0ms and 1000ms.
        /// </summary>
        WithoutPacketLossEnqueue = 4,
    }

    /// <summary>
    /// The TypeShipping is used to define the type of encryption of the bytes when being sent.
    /// </summary>
    public enum TypeEncrypt {
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

    class System{
        public static string? PublicKeyRSA, PrivateKeyRSA;
        public static byte[]? PrivateKeyAES;
        public static bool UnityBatchMode, RunningInUnity;
        public static ConcurrentQueue<Action> ListRunOnMainThread = new();
        public static Process Process = Process.GetCurrentProcess();
        public static HashSet<UDP.Client> ListClient = new();
        public static HashSet<UDP.Server> ListServer = new();
        static Aes AES = Aes.Create();
        static StreamWriter? fileLog;
        public static bool SaveLog = true;

        // Transform packets and send.
        public static void SendPacket(UdpClient? socket, byte[] bytes, int groupID, DataClient dataClient, TypeEncrypt typeEncrypt = TypeEncrypt.None, TypeShipping typeShipping = TypeShipping.None, IPEndPoint? ip = null, bool background = false){
            if(socket != null){
                bytes = BytesToSend(bytes, groupID, typeEncrypt, typeShipping, dataClient, background);
                if(bytes.Length > 1)
                    try{socket?.Send(bytes, bytes.Length, ip);}catch{}
            }
        }

        // Send packets without transform.
        public static void SendPing(UdpClient? socket, byte[] bytes, IPEndPoint? ip = null){
            if(socket != null && bytes.Length != 0)
                socket?.Send(bytes, bytes.Length, ip);
        }

        public static byte[] BytesToSend(byte[] bytes, int groupID, TypeEncrypt  typeEncrypt, TypeShipping typeShipping, DataClient dataClient, bool background = false){
            try{
                if(background){
                    // Compress RSA
                    if(groupID == 0)
                        bytes = Compress(bytes);

                    // Encrypt AES with AES
                    if(groupID == 1)
                        bytes = EncryptRSA(bytes, dataClient.PublicKeyRSA);
                }else{
                    // Encrypt
                    switch(typeEncrypt){
                        case TypeEncrypt.RSA:
                            bytes = EncryptRSA(bytes, dataClient.PublicKeyRSA);
                        break;
                        case TypeEncrypt.AES:
                            bytes = EncryptAES(bytes, dataClient.PrivateKeyAES);
                        break;
                        case TypeEncrypt.Base64:
                            bytes = EncryptBase64(bytes);
                        break;
                        case TypeEncrypt.OnlyBase64:
                            bytes = EncryptBase64(bytes);
                        break;
                        case TypeEncrypt.Compress:
                            bytes = Compress(bytes);
                        break;
                        case TypeEncrypt.OnlyCompress:
                            bytes = EncryptBase64(bytes);
                        break;
                    }
                }

                if(bytes.Length < 1)
                    return [];

                byte[] _groupID = BitConverter.GetBytes(groupID);
                byte[] _indexID = BitConverter.GetBytes(dataClient.IndexID);
                byte[] _bytes = new byte[bytes.Length + _indexID.Length + _groupID.Length + 4]; // 4 é a quantidade de dados

                //dados
                _bytes[0] = background ? (byte)0 : (byte)typeShipping;               // O tipo de envio
                _bytes[1] = (byte)typeEncrypt;                                       // O tipo de criptografia
                _bytes[2] = (byte)_groupID.Length;                                   // O tamanho do groupID
                _bytes[3] = (byte)_indexID.Length;                                   // O tamanho do index

                _groupID.CopyTo(_bytes, 4);                                     // GroupID
                _indexID.CopyTo(_bytes, 4 + _groupID.Length);                   // indexID
                bytes.CopyTo(_bytes, 4 + _indexID.Length + _groupID.Length);    // bytes

                // Hold Connection
                if(typeShipping == TypeShipping.WithoutPacketLoss)
                    dataClient.ListHoldConnection.TryAdd(dataClient.IndexID, _bytes);
                

                // Queuing Hold Connection
                if(typeShipping == TypeShipping.WithoutPacketLossEnqueue){
                    dataClient.QueuingHoldConnection.TryAdd(dataClient.IndexID, _bytes);
                    if(dataClient.QueuingHoldConnection.Count != 1){
                        dataClient.IndexID++;
                        return [];
                    }
                }

                dataClient.IndexID++;
                return _bytes;
            }
            catch{}
            return [];
        }

        static bool CheckDDOS(DataClient dataClient, bool background){
            long TimerNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            // background is allow only 1000ms for packets, to presev performance and atack DDOS
            if(dataClient.LimitMaxPPS == 0 && !background || TimerNow > dataClient.MaxPPSTimer + (background ? 1000 : (1000 / dataClient.LimitMaxPPS))){
                dataClient.MaxPPSTimer = TimerNow;
                return false;
            }
            return true;
        }

        public static (byte[], int, TypeEncrypt, int)? BytesToReceive(UdpClient socket, byte[] bytes, DataClient dataClient, IPEndPoint? ip = null){
            try{
                if(bytes.Length <= 1)
                    return null;

                // Receive respond hold connection
                if(bytes[0] == 1){
                    int indexID = BitConverter.ToInt16(bytes.Skip(2).Take(bytes[1]).ToArray());
                    dataClient.ListHoldConnection.TryRemove(indexID, out _);
                    if(dataClient.QueuingHoldConnection.TryRemove(indexID, out _))
                        if(dataClient.QueuingHoldConnection.Count > 0)
                            SendPing(socket, dataClient.QueuingHoldConnection.ElementAt(0).Value);
                
                    return null;
                }

                //bytes[0] = typeShipping
                //bytes[1] = typeEncrypt
                //bytes[2] = groupID size
                //bytes[3] = IndexID size
                byte[] _bytes = bytes.Skip(bytes[2] + bytes[3] + 4).ToArray();
                int _typeShipping = bytes[0];
                bool _background = bytes[0] == 0;
                TypeEncrypt _typeEncrypt = (TypeEncrypt)bytes[1];
                int _groupID = BitConverter.ToInt16(bytes.Skip(4).Take(bytes[2]).ToArray());
                int _indexID = BitConverter.ToInt16(bytes.Skip(4 + bytes[2]).Take(bytes[3]).ToArray());



                if(CheckDDOS(dataClient, _background))
                    return null;

                // Check if the packet is background
                if(_background){
                    // Decompress AES

                    if(_groupID == 0)
                        _bytes = Decompress(_bytes);

                    // Decompress RSA
                    if(_groupID == 1)
                        _bytes = DecryptRSA(_bytes, PrivateKeyRSA);
                }else{
                    // Decrypt
                    switch(_typeEncrypt){
                        case TypeEncrypt.RSA:
                            _bytes = DecryptRSA(_bytes, PrivateKeyRSA);
                        break;
                        case TypeEncrypt.AES:
                            _bytes = DecryptAES(_bytes, PrivateKeyAES);
                        break;
                        case TypeEncrypt.Base64:
                            _bytes = DecryptBase64(_bytes);
                        break;
                        case TypeEncrypt.Compress:
                            _bytes = Decompress(_bytes);
                        break;
                    }
                }

                if(_bytes == null)
                    return null;

                //bytes2[0] = code
                //bytes2[1] = IndexID size
                //bytes2[2] = indexID
                if(_typeShipping > 1){
                    byte[] _bytes2 = new byte[bytes[3] + 2];                                                // IndexID size + 2 slot
                    _bytes2[0] = 1;                                                                         // Code
                    _bytes2[1] = bytes[3];                                                                  // The size of indexID
                    bytes.Skip(4 + bytes[2]).Take(bytes[3]).ToArray().CopyTo(_bytes2, 2);                   // IndexID
                    // Hold Connection respond
                    SendPing(socket, _bytes2, ip);
                }
                
                // Check packets duplication
                if(!dataClient.ListIndex.Contains(_indexID))
                    dataClient.ListIndex.Add(_indexID);
                else
                    return null;
                
                return (_bytes, _groupID, _typeEncrypt, _typeShipping);
            }catch{}
            return null;
        }
        

        public static void GenerateKey(int SymmetricSize){
            if(PrivateKeyAES == null){
                using(var RSA = new RSACryptoServiceProvider()){
                    int value = (SymmetricSize - 6) * 8 + 384;
                    if(value >= 464 && value <= 4096){
                        PrivateKeyRSA = RSA.ToXmlString(true);
                        PublicKeyRSA = RSA.ToXmlString(false);
                        PrivateKeyAES = GetHashMD5(Encoding.ASCII.GetBytes(PrivateKeyRSA));
                    }else
                        throw new Nethostfire("RSA SymmetricSize cannot be less than " + ((464 - 384) / 8 + 6) + " or greater than " + ((4096 - 384) / 8 + 6));
                }
            }
        }

        private static byte[] EncryptRSA(byte[] bytes, string? publicKeyRSA){
            using(var RSA = new RSACryptoServiceProvider())
                try{
                    if(publicKeyRSA != null){
                        RSA.FromXmlString(publicKeyRSA);
                        return RSA.Encrypt(bytes, true);
                    }else
                        return [];
                }catch{
                    var b = ((RSA.KeySize - 384) / 8) + 6;
                    if(b < bytes.Length)
                        throw new Nethostfire("The key size defined in KeySizeBytesRSA, can only encrypt at most " + b + " bytes.");
                    return [];
                }
        }
        private static byte[] DecryptRSA(byte[] bytes, string? privateKeyRSA){
            using(var RSA = new RSACryptoServiceProvider())
            try{
                if(privateKeyRSA != null){
                    RSA.FromXmlString(privateKeyRSA);
                    return RSA.Decrypt(bytes, true);
                }else
                    return [];
            }catch{
                var b = ((RSA.KeySize - 384) / 8) + 6;
                if(b < bytes.Length)
                    throw new Nethostfire("The key size defined in KeySizeBytesRSA, can only decrypt at most " + b + " bytes.");
                return [];
            }
        }

        private static byte[] EncryptAES(byte[] bytes, byte[]? privateKeyAES){
            try{
                if(privateKeyAES != null){
                    using var encryptor = AES.CreateEncryptor(privateKeyAES, privateKeyAES);
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }else
                    return [];
            }
            catch{
                return [];
            }
        }
        private static byte[] DecryptAES(byte[] bytes, byte[]? privateKeyAES){
            try{
                if(privateKeyAES != null){
                    using var encryptor = AES.CreateDecryptor(privateKeyAES, privateKeyAES);
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }else
                    return [];
            }
            catch{
                return [];
            }
        }

        private static byte[] EncryptBase64(byte[] bytes){
            try{
                return Encoding.ASCII.GetBytes(Convert.ToBase64String(bytes));
            }catch{
                return [];
            }
        }

        private static byte[] DecryptBase64(byte[] bytes){
            try{
                return Convert.FromBase64String(Encoding.ASCII.GetString(bytes));
            }catch{
                return [];
            }
        }

        private static byte[] Compress(byte[] bytes){
            try{
                MemoryStream output = new();
                using(DeflateStream dstream = new(output, CompressionMode.Compress)){
                    dstream.Write(bytes, 0, bytes.Length);
                }
                return output.ToArray();
            }catch{
                return [];
            }
        }
        private static byte[] Decompress(byte[] data){
            try{
                MemoryStream input = new(data);
                MemoryStream output = new();
                using(DeflateStream dstream = new(input, CompressionMode.Decompress))
                    dstream.CopyTo(output);
                return output.ToArray();
            }catch{
                return [];
            }
        }

        private static byte[] GetHashMD5(byte[] bytes){
            try{
                MD5 md5 = MD5.Create();
                return md5.ComputeHash(bytes);
            }catch{
                return [];
            }
        }

        static int instance = 0;
        public static string Log(string message, bool SaveLog){
            if(SaveLog){
                if(!Directory.Exists("logs/"))
                    Directory.CreateDirectory("logs/");
                
                // Message text to bytes
                var text = DateTime.Now + " " + "[NETHOSTFIRE] " + message;
                // Location logs
                var filename = DateTime.Now.ToString("yyyy-MM-dd") +".log";


                // Generate new file if other program usage log file
                if(fileLog == null){
                    while(true)
                    try{
                        fileLog = new StreamWriter($"logs/{AppDomain.CurrentDomain.FriendlyName}-{instance}-{filename}", true, Encoding.UTF8){AutoFlush = true};
                        break;
                    }catch{
                        instance++;
                    }
                }

                fileLog.WriteLine(text);
            }
            
            if(RunningInUnity && !UnityBatchMode)
                ShowUnityLog(message);
            else{
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write(DateTime.Now + " ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("[NETHOSTFIRE] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
            }
            return message;
        }

        //================= Funções com dll da Unity =================

        public static void RunOnMainThread(Action _action){
            if(RunningInUnity)
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

        static void ShowUnityLog(string Message) => UnityEngine.Debug.Log("<color=red>[NETHOSTFIRE]</color> " + Message);

        public static void StartUnity(UDP.Client? client = null, UDP.Server? server = null){
            try{
                LoadUnity(client, server);
                RunningInUnity = true;
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    if(UnityBatchMode || !RunningInUnity)
                        Process.PriorityClass = ProcessPriorityClass.High;
            }catch{}
        }

        
        public static void LoadUnity(UDP.Client? client = null, UDP.Server? server = null){
            UnityBatchMode = UnityEngine.Application.isBatchMode;

            if(!UnityEngine.GameObject.Find("[Nethostfire]")){
                if(UnityBatchMode)
                    Console.Clear();
                new UnityEngine.GameObject("[Nethostfire]").AddComponent<NethostfireService>().hideFlags = UnityEngine.HideFlags.HideInHierarchy;
            }

            if(client != null && !ListClient.Contains(client))
                ListClient.Add(client);

            if(server != null && !ListServer.Contains(server))
                ListServer.Add(server);
        }
    }

    class Nethostfire : Exception{
        public Nethostfire (string message) : base(message){}
    }
}