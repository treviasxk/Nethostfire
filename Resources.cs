using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Nethostfire {
    public partial class Eventos: EventArgs{
        public delegate void OnReceivedNewDataServer(byte[] _byte, Type _type);
        public delegate void OnClientStatusConnection(ClientStatusConnection _status);
        public delegate void OnReceivedNewDataClient(byte[] _byte, Type _type, DataClient _dataClient);
        public delegate void OnServerStatusConnection(ServerStatusConnection _status);
        public delegate void OnConnectedClient(DataClient _dataClient);
        public delegate void OnDisconnectedClient(DataClient _dataClient);
    }
    public class DataClient{
        public IPEndPoint IP;
        public float Ping;
        public long Time;
        public string PublicKeyXML = "";
    }
    public enum ServerStatusConnection{
        Stopped = 0,
        Running = 1
    }
    public enum ClientStatusConnection{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
    }
}

class Resources{
    public static bool SaveLogError = true;
    public static string PrivateKeyXML, PublicKeyXML;
    public static Rijndael CryptClient = Rijndael.Create();
    public static RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
    public static void GenerateKeyRSA(){
        PrivateKeyXML = RSA.ToXmlString(true);
        PublicKeyXML = RSA.ToXmlString(false);
    }

    public static void AddLogError(Exception ex){
        try{
            if(SaveLogError)
                File.AppendAllText(Environment.CurrentDirectory + "/Nethostfire_ErrorLogs.txt", "["+ DateTime.Now +"]" + ex.StackTrace + Environment.NewLine);
        }catch{}
    }

    public static (byte[], Type) ByteToReceive(byte[] _byte){
        try{
            if(_byte[0] == 1){
                RSA.FromXmlString(PrivateKeyXML);
                byte[] type = new byte[_byte[1]];
                type = _byte.Skip(2).ToArray().Take(_byte[1]).ToArray();
                byte[] data = new byte[_byte.Length - type.Length - 2];
                _byte.Skip(2 + _byte[1]).ToArray().CopyTo(data,0);
                return (RSA.Decrypt(data, RSAEncryptionPadding.OaepSHA1), Type.GetType(Encoding.UTF8.GetString(type)));
            }else{
                byte[] type = new byte[_byte[1]];
                type = _byte.Skip(2).ToArray().Take(_byte[1]).ToArray();
                byte[] data = new byte[_byte.Length - type.Length - 2];
                _byte.Skip(2 + _byte[1]).ToArray().CopyTo(data,0);
                return (data, Type.GetType(Encoding.UTF8.GetString(type)));
            }
        }catch(Exception ex){
            AddLogError(ex);
            return (null, null);
        }
    }
    
    public static byte[] ByteToSend(byte[] _byte, Type _type, bool _encrypt){
        try{
            if(_encrypt){
                RSA.FromXmlString(PublicKeyXML);
                _byte = RSA.Encrypt(_byte, RSAEncryptionPadding.OaepSHA1);
                byte[] type = Encoding.UTF8.GetBytes(_type.ToString());
                byte[] data = new byte[_byte.Length + type.Length + 2];
                type.CopyTo(data, 2);
                _byte.CopyTo(data, 2 + type.Length);
                data[0] = 1;
                data[1] = (byte)type.Length;
                return data;
            }else{
                byte[] type = Encoding.UTF8.GetBytes(_type.ToString());
                byte[] data = new byte[_byte.Length + type.Length + 2];
                type.CopyTo(data, 2);
                _byte.CopyTo(data, 2 + type.Length);
                data[1] = (byte)type.Length;
                return data;
            }
        }catch(Exception ex){
            AddLogError(ex);
            return null;
        }
    }
}