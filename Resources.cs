// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Nethostfire {
    public class DataClient{
        public IPEndPoint IP;
        public string Ping;
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
    public class Utility{
        public static byte[] EncryptRSAByte(byte[] _byte, string _publicKeyXML){
            try{
                Resources.RSA.FromXmlString(_publicKeyXML);
                return Resources.RSA.Encrypt(_byte, true);
            }catch(Exception ex){
                Resources.AddLogError(ex);
                return new byte[]{};
            }
        }
        
        public static byte[] DecryptRSAByte(byte[] _byte){
            try{
                Resources.RSA.FromXmlString(Resources.PrivateKeyXML);
                return Resources.RSA.Decrypt(_byte, true);
            }catch(Exception ex){
                Resources.AddLogError(ex);
                return new byte[]{};
            }
        }

        public static byte[] CompressByte(byte[] _byte){
            try{
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress)){
                    dstream.Write(_byte, 0, _byte.Length);
                }
                return output.ToArray();
            }catch(Exception ex){
                Resources.AddLogError(ex);
                return new byte[]{};
            }
        }
        
        public static byte[] DecompressByte(byte[] data){
            try{
                MemoryStream input = new MemoryStream(data);
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress)){
                    dstream.CopyTo(output);
                }
                return output.ToArray();
            }catch(Exception ex){
                Resources.AddLogError(ex);
                return new byte[]{};
            }
        }
    }
}

class Resources{
    public static bool SaveLogError = false;
    public static string PrivateKeyXML = "", PublicKeyXML = "";
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

    public static (byte[], int) ByteToReceive(byte[] _byte){
        try{
            byte[] type = new byte[_byte[0]];
            type = _byte.Skip(1).ToArray().Take(_byte[0]).ToArray();
            byte[] data = new byte[_byte.Length - type.Length - 1];
            _byte.Skip(1 + _byte[0]).ToArray().CopyTo(data,0);
            return (data, BitConverter.ToInt32(type,0));
        }catch(Exception ex){
            AddLogError(ex);
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
        }catch(Exception ex){
            AddLogError(ex);
            return new byte[]{};
        }
    }
    public static void Send(UdpClient _udpClient, byte[] _byte, int _hashCode, Nethostfire.DataClient _dataClient = null){
        try{
            byte[] buffer = Resources.ByteToSend(_byte, _hashCode);
            if(_dataClient == null)
                _udpClient.Send(buffer, buffer.Length);
            else
                _udpClient.Send(buffer, buffer.Length, _dataClient.IP);
        }catch{}
    }
    public static void SendPing(UdpClient _udpClient, byte[] _byte, Nethostfire.DataClient _dataClient = null){
        try{
            if(_dataClient == null)
                _udpClient.Send(_byte, _byte.Length);
            else
                _udpClient.Send(_byte, _byte.Length, _dataClient.IP);
        }catch{}
    }
}