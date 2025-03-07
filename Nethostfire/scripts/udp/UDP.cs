// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Net;
using System.Net.Sockets;
using static Nethostfire.DataSecurity;

namespace Nethostfire {
    public partial class UDP{

        public static void SendPing(UdpClient? socket, byte[] bytes, IPEndPoint? ip = null){
            if(socket != null && bytes.Length != 0)
                socket?.Send(bytes, bytes.Length, ip);
        }

        static void SendPacket(UdpClient? socket, byte[]? bytes, int groupID, TypeEncrypt typeEncrypt, ref Session clientSession, IPEndPoint? ip = null){
            if(socket != null && bytes != null){
                bytes = ConvertPacket(bytes, groupID, typeEncrypt, ref clientSession);
                if(bytes != null && bytes.Length > 1)
                    try{socket?.Send(bytes, bytes.Length, ip); clientSession.Index++;}catch{}
            }
        }

        static (byte[], int)? DeconvertPacket(byte[] bytes, ref Session session){
            if(bytes.Length == 1)
                return (bytes, 0);

            // bytes[0] = (byte)typeEncrypt;
            // bytes[1] = (byte)index.Length;
            // bytes[2] = (byte)group.Length;
            try{
                TypeEncrypt typeEncrypt = (TypeEncrypt)bytes[0];
                int index = BitConverter.ToInt16(bytes.Skip(3).Take(bytes[1]).ToArray());
                int group = BitConverter.ToInt16(bytes.Skip(3 + bytes[1]).Take(bytes[2]).ToArray());
                bytes = bytes.Skip(3 + bytes[1] + bytes[2]).ToArray();

                // decryptograph
                switch(typeEncrypt){
                    case TypeEncrypt.Compress:
                        bytes = Decompress(bytes);
                    break;
                    case TypeEncrypt.RSA:
                        bytes = DecryptRSA(bytes, PrivateKeyRSA);
                    break;
                    case TypeEncrypt.Base64:
                        bytes = DecryptBase64(bytes);
                    break;
                    case TypeEncrypt.AES:
                        bytes = DecryptAES(bytes, PrivateKeyAES);
                    break;
                }

                return (bytes, group);
            }catch{

            }
            return null;
        }

        static byte[] ConvertPacket(byte[] bytes, int groupID, TypeEncrypt typeEncrypt, ref Session session){
            // Cryptograph
            switch(typeEncrypt){
                case TypeEncrypt.Compress:
                    bytes = Compress(bytes);
                break;
                case TypeEncrypt.RSA:
                    bytes = EncryptRSA(bytes, session.Credentials.PublicKeyRSA);
                break;
                case TypeEncrypt.Base64:
                    bytes = EncryptBase64(bytes);
                break;
                case TypeEncrypt.AES:
                    bytes = EncryptAES(bytes, PrivateKeyAES);
                break;
                case TypeEncrypt.OnlyCompress:
                    bytes = Compress(bytes);
                break;
                case TypeEncrypt.OnlyBase64:
                    bytes = EncryptBase64(bytes);
                break;
            }

            var index = BitConverter.GetBytes(session.Index);
            var group = BitConverter.GetBytes(groupID);
            var data = new byte[index.Length + group.Length + bytes.Length + 3];

            data[0] = (byte)typeEncrypt;
            data[1] = (byte)index.Length;
            data[2] = (byte)group.Length;

            index.CopyTo(data, 3);
            group.CopyTo(data, 3 + index.Length);
            bytes.CopyTo(data, 3 + index.Length + group.Length);

            return data;
        }
    }
}