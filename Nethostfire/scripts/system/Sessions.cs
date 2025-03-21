// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Collections.Concurrent;
using System.Net;

namespace Nethostfire {

    public class Sessions{
        public ConcurrentDictionary<IPEndPoint, Session> Clients = new();
        public bool TryAdd(IPEndPoint key, Session value) => Clients.TryAdd(key, value);


        public SessionStatus GetStatus(IPEndPoint ip){
            Clients.TryGetValue(ip, out Session session);
            return session.Status;
        }

        public bool TryGetValue(IPEndPoint key, out Session value){
            if(Clients.TryGetValue(key, out Session foundValue)){
                value = foundValue; // Usa o valor encontrado
                return true;
            }else
                value = new(){retransmissionBuffer = new(), Status = SessionStatus.Disconnected};
            return false;
        }

        public bool TryUpdate(IPEndPoint key, in Session value){
            if(Clients.TryGetValue(key, out var client)){
                Clients[key] = value;
                return true;
            }
            return false;
        }

        public bool TryRemove(IPEndPoint key) => Clients.TryRemove(key, out _);
        public void Clear() => Clients.Clear();
    }

    public struct Credentials{
        // Public key RSA to encrypt bytes
        public string PublicKeyRSA;
        // Private key AES to encrypt bytes
        public byte[] PrivateKeyAES;
    }

    public struct Session{
        public ushort Ping;
        public SessionStatus Status;
        public Credentials Credentials;
        
        internal int Index;
        // Timer to check if client is connected
        internal long Timer;
        internal long TimerReceivedPPS;
        internal long TimerSendPPS;
        internal ConcurrentDictionary<int, byte[]> retransmissionBuffer;
    }
}