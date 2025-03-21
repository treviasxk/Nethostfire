// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Nethostfire {
    public enum MySQLStatus {
        Connected,
        Connecting,
        Disconnected,
    }

    public enum ServerStatus{
        Stopped = 0,
        Stopping = 1,
        Running = 2,
        Initializing = 3,
        Restarting = 4,
    }

    public enum SessionStatus{
        Disconnected = 0,
        Disconnecting = 1,
        Connected = 2,
        Connecting = 3,
        Kicked = 4,
        IpBlocked = 5,
        MaxClientExceeded = 6,
    }

    /// <summary>
    /// The TypeHoldConnection is a feature to guarantee the sending of udp packets even with packet losses.
    /// </summary>
    public enum TypeShipping {
        // 0 = background
        // 1 = Respond
        None = 2,
        /// <summary>
        /// With WithoutPacketLoss, bytes are sent to their destination without packet loss, shipments will not be queued to improve performance.
        /// </summary>
        WithoutPacketLoss = 3,
        /// <summary>
        /// With WithoutPacketLossEnqueue, bytes are sent to their destination without packet loss, shipments will be sent in a queue, this feature is not recommended to be used for high demand for shipments, each package can vary between 0ms and 1000ms.
        /// </summary>
        WithoutPacketLossEnqueue = 4,
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

    class System{
        public static ConcurrentQueue<Action> ListRunOnMainThread = new();
        public static Process Process = Process.GetCurrentProcess();
        public static HashSet<UDP.Client> ListClient = new();
        public static HashSet<UDP.Server> ListServer = new();
        static StreamWriter? fileLog;
        static int currentInstance = 0;
        public static bool SaveLog = true;

        public static ushort GetPing(long Timer){
            var ping = ((DateTime.Now.Ticks - Timer) / TimeSpan.TicksPerMillisecond) - 1000;
            return Convert.ToUInt16(ping >= ushort.MinValue && ping <= ushort.MaxValue ? ping : 0);
        }

        public static void RunOnMainThread(Action action) => ListRunOnMainThread.Enqueue(action);

        public static void WriteLog(object? message, object? instance = null, bool showLog = true) => WriteLog(message?.ToString() ?? "", instance, showLog);
        public static void WriteLog(string message, object? instance = null, bool showLog = true){
            string InstanceName = instance == null ? "" : $"[{instance.GetType().Name.ToUpper()}] "; 

            if(SaveLog){
                if(!Directory.Exists("logs/"))
                    Directory.CreateDirectory("logs/");

                // Message text to bytes
                var text = $"{DateTime.Now} [NETHOSTFIRE] {InstanceName}{message}";
                // Location logs
                var filename = DateTime.Now.ToString("yyyy-MM-dd") +".log";


                // Generate new file if other program usage log file
                if(fileLog == null){
                    while(true)
                    try{
                        fileLog = new StreamWriter($"logs/{AppDomain.CurrentDomain.FriendlyName}-{currentInstance}-{filename}", true, Encoding.UTF8){AutoFlush = true};
                        break;
                    }catch{
                        currentInstance++;
                    }
                }

                fileLog.WriteLine(text);
            }
            
            if(showLog){
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write(DateTime.Now);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" [NETHOSTFIRE] {InstanceName}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
            }
        }
    }

    class Nethostfire : Exception{
        public Nethostfire (string message, object? instance = null) : base(message){
            System.WriteLog(message, true, false);
        }
    }
}