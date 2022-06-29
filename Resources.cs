// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace Nethostfire {
    public class DataClient{
        /// <summary>
        /// IP do Client.
        /// </summary>
        public IPEndPoint IP;
        /// <summary>
        /// Agrupador de Pacotes da Internet, ping (ms).
        /// </summary>
        public string Ping;
        /// <summary>
        /// Ultimo tempo atualizado pelo o servidor.
        /// </summary>
        public long Time;
        /// <summary>
        /// Chave publica de criptografia RSA.
        /// </summary>
        public string PublicKeyXML = "";
    }
    public enum ServerStatusConnection{
        Stopped = 0,
        Stopping = 1,
        Running = 2,
        Initializing = 3,
        Restarting = 4
    }
    public enum ClientStatusConnection{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        NoConnection = 4
    }

    public class Utility{
        static readonly ConcurrentQueue<Action> ListRunOnMainThread = new ConcurrentQueue<Action>();
        /// <summary>
        /// Executa ações dentro da thread principal do software, é utilizado para manipular objetos 3D na Unity.
        /// </summary>
        public static void RunOnMainThread(Action action){
            ListRunOnMainThread.Enqueue(action);
        }
        /// <summary>
        /// Utilizado para definir a thread principal que irá executar as ações do RunOnMainThread(). Coloque essa ação dentro da função void Update() na Unity.
        /// </summary>
        public static void ThisMainThread() {
            if (!ListRunOnMainThread.IsEmpty) {
                while (ListRunOnMainThread.TryDequeue(out var action)) {
                    try{
                        action?.Invoke();
                    }catch{
                        throw;
                    }
                }
            }
        }
        /// <summary>
        /// Criptografar bytes em MD5.
        /// </summary>
        public static string EncryptMD5Byte(byte[] _byte){
            try{
                MD5 md5 = MD5.Create();
                return BitConverter.ToString(md5.ComputeHash(_byte)).Replace("-", string.Empty);
            }catch{
                return "";
            }
        }
        /// <summary>
        /// Criptografar bytes com RSA.
        /// </summary>
        public static byte[] EncryptRSAByte(byte[] _byte, string _publicKeyXML){
            try{
                Resources.RSA.FromXmlString(_publicKeyXML);
                return Resources.RSA.Encrypt(_byte, true);
            }catch{
                return new byte[]{};
            }
        }
        /// <summary>
        /// Descriptografar bytes com RSA.
        /// </summary>
        public static byte[] DecryptRSAByte(byte[] _byte){
            try{
                Resources.RSA.FromXmlString(Resources.PrivateKeyXML);
                return Resources.RSA.Decrypt(_byte, true);
            }catch{
                return new byte[]{};
            }
        }
        /// <summary>
        /// Compactar bytes.
        /// </summary>
        public static byte[] CompressByte(byte[] _byte){
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
        /// <summary>
        /// Descompactar bytes.
        /// </summary>
        public static byte[] DecompressByte(byte[] data){
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
    }
}

class Resources{
    public static string PrivateKeyXML = "", PublicKeyXML = "";
    public static RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
    public static void GenerateKeyRSA(){
        PrivateKeyXML = RSA.ToXmlString(true);
        PublicKeyXML = RSA.ToXmlString(false);
    }

    public static (byte[], int) ByteToReceive(byte[] _byte){
        try{
            byte[] type = new byte[_byte[0]];
            type = _byte.Skip(1).ToArray().Take(_byte[0]).ToArray();
            byte[] data = new byte[_byte.Length - type.Length - 1];
            _byte.Skip(1 + _byte[0]).ToArray().CopyTo(data,0);
            return (data, BitConverter.ToInt32(type,0));
        }catch{
            return (new byte[]{}, 0);
        }
    }
    
    public static byte[] ByteToSend(byte[] _byte, int _hashCode){
        try{
            byte[] type = BitConverter.GetBytes(_hashCode);
            byte[] data = new byte[_byte.Length + type.Length + 1];
            type.CopyTo(data, 1);
            _byte.CopyTo(data, 1 + type.Length);
            data[0] = (byte)type.Length;
            return data;
        }catch{
            return new byte[]{};
        }
    }
    public static bool Send(UdpClient _udpClient, byte[] _byte, int _hashCode, Nethostfire.DataClient _dataClient = null){
        try{
            byte[] buffer = Resources.ByteToSend(_byte, _hashCode);
            if(_dataClient == null)
                _udpClient.Send(buffer, buffer.Length);
            else
                _udpClient.Send(buffer, buffer.Length, _dataClient.IP);
            return true;
        }catch{
            return false;
        }
    }
    public static bool SendPing(UdpClient _udpClient, byte[] _byte, Nethostfire.DataClient _dataClient = null){
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
}