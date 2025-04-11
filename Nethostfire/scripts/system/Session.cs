// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Collections.Concurrent;

namespace Nethostfire {
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
        internal long TimerReceivedPPS;
        internal long TimerSendPPS;
        internal ConcurrentDictionary<int, byte[]> retransmissionBuffer = new();
    }
}