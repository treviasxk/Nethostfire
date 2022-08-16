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
        public string PublicKeyXML = "";
    }
    public enum ServerStatusConnection{
        Stopped = 0,
        Stopping = 1,
        Running = 2,
        Initializing = 3,
        Restarting = 4,
        FailedInitialize = 5
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
        public static string PrivateKeyXML = "", PublicKeyXML = "";
        public static RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
        public static void GenerateKeyRSA(){
            PrivateKeyXML = RSA.ToXmlString(true);
            PublicKeyXML = RSA.ToXmlString(false);
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
        

        public static (byte[], int, bool) ByteToReceive(byte[] _byte, UdpClient _udpClient, DataClient _dataClient = null){
            try{
                bool _holdConnection = false;
                byte[] type = new byte[_byte[1]]; // 0
                type = _byte.Skip(2).ToArray().Take(_byte[1]).ToArray(); // 1 0
                byte[] data = new byte[_byte.Length - type.Length - 2]; // 1
                _byte.Skip(2 + _byte[1]).ToArray().CopyTo(data,0); // 1 0 0

                if(_byte[0] == 1){
                    byte[] data2 = new byte[_byte[1] + 2];
                    data2[0] = 2;
                    data2[1] = _byte[1];
                    type.CopyTo(data2, 2); // 1
                    SendPing(_udpClient, data2, _dataClient);
                }
                if(_byte[0] == 2){
                    _holdConnection = true;
                }
                return (data, BitConverter.ToInt32(type,0), _holdConnection);
            }catch{
                return (new byte[]{}, 0, false);
            }
        }
        
        public static byte[] ByteToSend(byte[] _byte, int _hashCode, bool _holdConnection){
            try{
                byte[] type = BitConverter.GetBytes(_hashCode);
                byte[] data = new byte[_byte.Length + type.Length + 2]; // 1
                type.CopyTo(data, 2); // 1
                _byte.CopyTo(data, 2 + type.Length); // 1
                data[1] = (byte)type.Length; // 0
                data[0] = (byte)(_holdConnection ? 1 : 0);
                return data;
            }catch{
                return new byte[]{};
            }
        }
        public static bool Send(UdpClient _udpClient, byte[] _byte, int _hashCode, bool _holdConnection, Nethostfire.DataClient _dataClient = null){
            try{
                byte[] buffer = Resources.ByteToSend(_byte, _hashCode, _holdConnection);
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
}