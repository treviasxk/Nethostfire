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
        Restarting = 4
    }
    public enum ClientStatusConnection{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        NoConnection = 4
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
}