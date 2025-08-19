// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using static Nethostfire.DataSecurity;

namespace Nethostfire.UDP {
    public enum SessionStatus{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        Kicked = 4,
        IpBlocked = 5,
        MaxClientExceeded = 6,
    }

    public enum ServerState{
        Stopped = 0,
        Stopping = 1,
        Running = 2,
        Initializing = 3,
        Restarting = 4,
    }


    /// <summary>
    /// The TypeHoldConnection is a feature to guarantee the sending of udp packets even with packet losses.
    /// </summary>
    public enum TypeShipping {
        // 0 = Resply
        None = 1,
        /// <summary>
        /// With WithoutPacketLoss, bytes are sent to their destination without packet loss, shipments will not be queued to improve performance.
        /// </summary>
        WithoutPacketLoss = 2,
        /// <summary>
        /// With WithoutPacketLossEnqueue, bytes are sent to their destination without packet loss, shipments will be sent in a queue, this feature is not recommended to be used for high demand for shipments, each package can vary between 0ms and 1000ms.
        /// </summary>
        WithoutPacketLossEnqueue = 3,
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
    public class Session{
        public ushort Ping;
        public SessionStatus Status;
        // Public key RSA to encrypt bytes
        public string PublicKeyRSA = "";
        // Private key AES to encrypt bytes
        public byte[]? PrivateKeyAES = null;
        internal int Index;
        internal int IndexShipping;
        // Timer to check if client is connected
        internal long Timer;
        internal long TimerPing;
        internal long TimerReceivedPPS;
        internal long TimerSendPPS;
        internal ConcurrentDictionary<int, byte[]> retransmissionBuffer = new();
    }

    internal class UDP{
        public static bool CheckReceiveLimitPPS(in byte[] bytes, in int groupID, ref Session session, int LimitPPS, in ConcurrentDictionary<int, int>? LimitGroudIdPPS = null){
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

        public static void SendPacket(UdpClient? socket, ref byte[]? bytes, int groupID, TypeEncrypt typeEncrypt, TypeShipping typeShipping, ref Session session, in IPEndPoint? ip = null, in ConcurrentDictionary<int, int>? ListSendGroupIdPPS = null){
            if(socket != null && (session.Status == SessionStatus.Connected || session.Status == SessionStatus.Connecting)){
                if(CheckSendLimitPPS(groupID, ref session, ListSendGroupIdPPS)){
                    bytes = ConvertPacket(bytes, groupID, typeEncrypt, typeShipping, ref session);
                    if(typeShipping == TypeShipping.WithoutPacketLoss)
                        session.retransmissionBuffer.TryAdd(session.Index, bytes);
                    if(bytes != null && bytes.Length > 1)
                        try{socket?.Send(bytes, bytes.Length, ip);}catch{}
                }
            }
        }

        public static (byte[], int)? DeconvertPacket(UdpClient socket, byte[] bytes, ref Session session, IPEndPoint? ip){
            if(bytes.Length == 1)
                return (bytes, 0);

            // bytes[0] = (byte)typeEncrypt;
            // bytes[1] = (byte)typeShipping;
            // bytes[2] = (byte)index.Length;
            // bytes[3] = (byte)group.Length;
            try{
                if(bytes[0] == 0 && bytes.Length == 5){
                    var id = BitConverter.ToInt32(bytes, 1);
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


                if(typeShipping == TypeShipping.WithoutPacketLoss || typeShipping == TypeShipping.WithoutPacketLossEnqueue){
                    // Check if the packet is in the retransmission buffer
                    var id = BitConverter.GetBytes(index);
                    var reply = new byte[id.Length + 1];
                    id.CopyTo(reply, 1);
                    SendPing(socket, reply, ip);

                    if(typeShipping == TypeShipping.WithoutPacketLossEnqueue){
                        if(session.IndexShipping == index){
                            session.IndexShipping++;
                            return (bytes, group);
                        }else
                            return null;
                    }
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

            var index = BitConverter.GetBytes(typeShipping == TypeShipping.WithoutPacketLossEnqueue ? session.IndexShipping++ : session.Index++);
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