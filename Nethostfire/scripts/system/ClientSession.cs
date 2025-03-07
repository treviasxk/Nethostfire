// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Collections.Concurrent;
using System.Net;

namespace Nethostfire {

    class Sessions{
        public ConcurrentDictionary<IPEndPoint, Session> data = new();
        public bool TryAdd(IPEndPoint key, Session value) => data.TryAdd(key, value);
        public bool TryGetOrUpdateValue(IPEndPoint key, ref Session value){
            if(data.TryGetValue(key, out Session foundValue)){
                value = foundValue; // Usa o valor encontrado
                return true;
            }
            return false;
        }

        public bool TryUpdate(IPEndPoint key, in Session value){
            if(data.TryGetValue(key, out _)){
                data[key] = value;
                return true;
            }
            return false;
        }

        public bool TryRemove(IPEndPoint key) => data.TryRemove(key, out _);

        public void Clear() => data.Clear();
    }

    struct Credentials{
        // Public key RSA to encrypt bytes
        public string PublicKeyRSA;
        // Private key AES to encrypt bytes
        public byte[] PrivateKeyAES;
    }

    struct Session{
        public int Index;
        public ushort Ping;
        // Timer to check if client is connected
        public long Timer;
        public Credentials Credentials;
        public ConcurrentDictionary<int, byte[]> retransmissionBuffer;
    }
}