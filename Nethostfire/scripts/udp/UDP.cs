// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using static Nethostfire.DataSecurity;

namespace Nethostfire {
    public partial class UDP{

        static bool CheckReceiveLimitPPS(in byte[] bytes, in int groupID, ref Session session, int LimitPPS, in ConcurrentDictionary<int, int>? LimitGroudIdPPS = null){
            if(LimitPPS == 0 && LimitGroudIdPPS?.Count == 0)
                return true;

            // Check limit PPS and Bandwidth
            if(DateTime.Now.Ticks > session.TimerReceivedPPS + (LimitPPS > 0 ? (1000 / LimitPPS * TimeSpan.TicksPerMillisecond) : 0)){
                var limit = 0;
                LimitGroudIdPPS?.TryGetValue(groupID, out limit);
                // Check limit Group PPS
                if(DateTime.Now.Ticks > session.TimerReceivedPPS + (limit > 0 ? (1000 / limit * TimeSpan.TicksPerMillisecond) : 0)){
                    session.TimerReceivedPPS = DateTime.Now.Ticks;
                    return true;
                }
            }
            return false;
        }

        static bool CheckSendLimitPPS(int groupID, ref Session session, in ConcurrentDictionary<int, int>? ListReceiveGroupIdPPS = null){
            var limit = 0;
            ListReceiveGroupIdPPS?.TryGetValue(groupID, out limit);
            if(DateTime.Now.Ticks > session.TimerSendPPS + (limit > 0 ? (1000 / limit * TimeSpan.TicksPerMillisecond) : 0)){
                if(limit > 0)
                    session.TimerSendPPS = DateTime.Now.Ticks;

                return true;
            }
            return false;
        }

        public static void SendPing(UdpClient? socket, byte[] bytes, IPEndPoint? ip = null){
            if(socket != null && bytes.Length != 0)
                socket?.Send(bytes, bytes.Length, ip);
        }

        static void SendPacket(UdpClient? socket, ref byte[]? bytes, int groupID, TypeEncrypt typeEncrypt, TypeShipping typeShipping, ref Session session, in IPEndPoint? ip = null, in ConcurrentDictionary<int, int>? ListSendGroupIdPPS = null){
            if(socket != null && (session.Status == SessionStatus.Connected || session.Status == SessionStatus.Connecting)){
                if(CheckSendLimitPPS(groupID, ref session, ListSendGroupIdPPS)){
                    bytes = ConvertPacket(bytes, groupID, typeEncrypt, typeShipping, ref session);
                    if(typeShipping == TypeShipping.WithoutPacketLoss)
                        session.retransmissionBuffer.TryAdd(session.Index, bytes);
                    if(bytes != null && bytes.Length > 1)
                        try{socket?.Send(bytes, bytes.Length, ip); session.Index++;}catch{}
                }
            }
        }

        static (byte[], int)? DeconvertPacket(UdpClient socket, byte[] bytes, ref Session session, IPEndPoint? ip){
            if(bytes.Length == 1)
                return (bytes, 0);

            // bytes[0] = (byte)typeEncrypt;
            // bytes[1] = (byte)typeShipping;
            // bytes[2] = (byte)index.Length;
            // bytes[3] = (byte)group.Length;
            try{
                if(bytes[0] == 0 && bytes.Length == 5){
                    var id = BitConverter.ToInt32(bytes, 1);
                    Console.WriteLine($"Retransmission: {id}");
                    session.retransmissionBuffer.TryRemove(id, out _);
                    return null;
                }


                TypeEncrypt typeEncrypt = (TypeEncrypt)bytes[0];
                TypeShipping typeShipping = (TypeShipping)bytes[1];

                int index = BitConverter.ToInt16(bytes.Skip(4).Take(bytes[2]).ToArray());
                int group = BitConverter.ToInt16(bytes.Skip(4 + bytes[2]).Take(bytes[3]).ToArray());
                bytes = bytes.Skip(4 + bytes[2] + bytes[3]).ToArray();

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

                switch(typeShipping){
                    case TypeShipping.WithoutPacketLoss:
                        var id = BitConverter.GetBytes(session.Index);
                        var reply = new byte[id.Length + 1];
                        id.CopyTo(reply, 1);
                        SendPing(socket, reply, ip);
                    break;
                    case TypeShipping.WithoutPacketLossEnqueue:
                        
                    break;
                }


                return (bytes, group);
            }catch{}
            return null;
        }

        static byte[] ConvertPacket(byte[]? bytes, int groupID, TypeEncrypt typeEncrypt, TypeShipping typeShipping, ref Session session){
            bytes ??= [];
            // Cryptograph
            switch(typeEncrypt){
                case TypeEncrypt.Compress:
                    bytes = Compress(bytes);
                break;
                case TypeEncrypt.RSA:
                    bytes = EncryptRSA(bytes, session.PublicKeyRSA);
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

            // bytes[0] = (byte)typeEncrypt;
            // bytes[1] = (byte)typeShipping;
            // bytes[2] = (byte)index.Length;
            // bytes[3] = (byte)group.Length;

            var index = BitConverter.GetBytes(session.Index);
            var group = BitConverter.GetBytes(groupID);
            var data = new byte[index.Length + group.Length + bytes.Length + 4];

            data[0] = (byte)typeEncrypt;
            data[1] = (byte)typeShipping; // typeShipping
            data[2] = (byte)index.Length;
            data[3] = (byte)group.Length;


            index.CopyTo(data, 4);
            group.CopyTo(data, 4 + index.Length);
            bytes.CopyTo(data, 4 + index.Length + group.Length);

            return data;
        }
    }
}