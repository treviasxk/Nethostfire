// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

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
        public string PublicKeyRSA = "";
        public byte[] PrivateKeyAES = null;
    }
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
    public enum TypeEncrypt{
        None = 0,
        AES = 1,
        RSA = 2,
        Base64 = 3,
        Compress = 4,
    }

    public enum ClientStatusConnection{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        NoConnection = 4
    }

    class HoldConnectionServer{
        public List<int> HashCode {get;set;}
        public List<byte[]> Bytes {get;set;}
        public List<long> Time {get;set;}
    }
    class HoldConnectionClient{
        public byte[] Bytes {get;set;}
        public long Time {get;set;}
    }
    class Resources{
        public static string PrivateKeyRSA = "", PublicKeyRSA = "";
        public static byte[] KeyAES;
        public static RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
        public static void GenerateKeyRSA(){
            PrivateKeyRSA = RSA.ToXmlString(true);
            PublicKeyRSA = RSA.ToXmlString(false);
            KeyAES = Utility.GetHashMD5(System.Text.Encoding.ASCII.GetBytes(PrivateKeyRSA));
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

        public static (byte[], int, bool, TypeContent, TypeEncrypt) ByteToReceive(byte[] _byte, UdpClient _udpClient, DataClient _dataClient = null){
            try{
                bool _holdConnection = false;
                byte[] hascode = new byte[_byte[3]];
                hascode = _byte.Skip(4).ToArray().Take(_byte[3]).ToArray();
                byte[] data = new byte[_byte.Length - hascode.Length - 4];
                _byte.Skip(4 + _byte[3]).ToArray().CopyTo(data,0);

                TypeEncrypt _typeEncrypt = (TypeEncrypt)_byte[2];
                TypeContent _typeContent = (TypeContent)_byte[1];

                if(_typeContent == TypeContent.Background)
                    switch(_typeEncrypt){
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
                            data = Utility.DecryptBase64(System.Text.Encoding.ASCII.GetString(data));
                        break;
                        case TypeEncrypt.Compress:
                            data = Utility.Decompress(data);
                        break;
                    }

                if(_byte[0] == 1){
                    byte[] data2 = new byte[_byte[3] + 4];
                    data2[0] = 2;
                    data2[1] = 0;
                    data2[2] = 0;
                    data2[3] = _byte[3];
                    hascode.CopyTo(data2, 4);
                    SendPing(_udpClient, data2, _dataClient);
                }
                if(_byte[0] == 2)
                    _holdConnection = true;

                return (data, BitConverter.ToInt32(hascode,0), _holdConnection, _typeContent, _typeEncrypt);
            }catch{
                return (new byte[]{}, 0, false, TypeContent.Foreground, TypeEncrypt.None);
            }
        }
        
        public static byte[] ByteToSend(byte[] _byte, int _hashCode, TypeEncrypt _typeEncrypt, bool _holdConnection, TypeContent _typeContent, DataClient _dataClient = null){
            try{
                if(_typeContent == TypeContent.Background)
                    switch(_typeEncrypt){
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
                        _byte = System.Text.Encoding.ASCII.GetBytes(Utility.EncryptBase64(_byte));
                    break;
                    case TypeEncrypt.Compress:
                        _byte = Utility.Compress(_byte);
                    break;
                }

                byte[] hascode = BitConverter.GetBytes(_hashCode);
                byte[] data = new byte[_byte.Length + hascode.Length + 4];

                data[0] = (byte)(_holdConnection ? 1 : 0);          // Se é um Hold Connection
                data[1] = (byte)_typeContent;                       // O tipo de conteúdo
                data[2] = (byte)_typeEncrypt;                       // O tipo de criptografia
                data[3] = (byte)hascode.Length;                     // O tamanho do hascode

                hascode.CopyTo(data, 4);
                _byte.CopyTo(data, 4 + hascode.Length);

                return data;
            }catch{
                return new byte[]{};
            }
        }
        public static bool Send(UdpClient _udpClient, byte[] _byte, int _hashCode, TypeEncrypt _typeEncrypt, bool _holdConnection, TypeContent _typeContent, DataClient _dataClient = null){
            try{
                byte[] buffer = Resources.ByteToSend(_byte, _hashCode, _typeEncrypt, _holdConnection, _typeContent, _dataClient);
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
        public static byte[] EncryptRSA(byte[] _byte, DataClient _dataClient){
            try{
                Resources.RSA.FromXmlString(_dataClient != null ? _dataClient.PublicKeyRSA : Client.PublicKeyRSA);
                return Resources.RSA.Encrypt(_byte, true);
            }catch{
                Console.WriteLine("oiiii");
                return new byte[]{};
            }
        }
        public static byte[] DecryptRSA(byte[] _byte){
            try{
                Resources.RSA.FromXmlString(Resources.PrivateKeyRSA);
                return Resources.RSA.Decrypt(_byte, true);
            }catch{
                return new byte[]{};
            }
        }
        public static byte[] EncryptAES(byte[] _byte, DataClient _dataClient = null){
            var key = _dataClient != null ? _dataClient.PrivateKeyAES : Resources.KeyAES;
            using (var aes = Aes.Create())
            using (var encryptor = aes.CreateEncryptor(key, key))
                return encryptor.TransformFinalBlock(_byte, 0, _byte.Length);
        }
        public static byte[] DecryptAES(byte[] _byte, DataClient _dataClient = null){
            var key = _dataClient != null ? _dataClient.PrivateKeyAES : Resources.KeyAES;
            using (var aes = Aes.Create())
            using (var encryptor = aes.CreateDecryptor(key, key))
                return encryptor.TransformFinalBlock(_byte, 0, _byte.Length);
        }
    }
}