// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Text;

namespace Nethostfire {
    public enum MySQLState {
        Connected,
        Connecting,
        Disconnected,
    }

    partial class Nethostfire
    {
        static StreamWriter? fileLog;
        static int currentInstance = 0;
        public static bool SaveLog = true;
        public static bool UnityBatchMode = false, RunningInUnity = false;

        internal static ushort GetPing(long Timer)
        {
            var ping = ((DateTime.Now.Ticks - Timer) / TimeSpan.TicksPerMillisecond) - 1000;
            return Convert.ToUInt16(ping >= ushort.MinValue && ping <= ushort.MaxValue ? ping : 0);
        }

        internal static void LoadUnity()
        {
            UnityBatchMode = UnityEngine.Application.isBatchMode;
        }

        internal static void StartUnity()
        {
            try
            {
                LoadUnity();
                RunningInUnity = true;
            }
            catch { }
        }

        internal static void RunParallel(Action action) {
            Parallel.Invoke(() => {
                action?.Invoke();
            });
        } 

        public static void WriteLog(object? message, object? instance = null, bool showLog = true) => WriteLog(message?.ToString() ?? "", instance, showLog);
        public static void WriteLog(string message, object? instance = null, bool showLog = true)
        {
            string InstanceName = instance == null ? "" : $"[{instance.GetType().Name.ToUpper()}] ";

            if (SaveLog)
            {
                if (!Directory.Exists("logs/"))
                    Directory.CreateDirectory("logs/");

                // Message text to bytes
                var text = $"{DateTime.Now} [NETHOSTFIRE] {InstanceName}{message}";
                // Location logs
                var filename = DateTime.Now.ToString("yyyy-MM-dd") + ".log";


                // Generate new file if other program usage log file
                if (fileLog == null)
                {
                    while (true)
                        try
                        {
                            fileLog = new StreamWriter($"logs/{AppDomain.CurrentDomain.FriendlyName}-{currentInstance}-{filename}", true, Encoding.UTF8) { AutoFlush = true };
                            break;
                        }
                        catch
                        {
                            currentInstance++;
                        }
                }

                fileLog.WriteLine(text);
            }

            if (showLog)
                if (RunningInUnity && !UnityBatchMode)
                    ShowUnityLog(message);
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(DateTime.Now);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($" [NETHOSTFIRE] {InstanceName}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(message);
                }
        }
        
        static void ShowUnityLog(string Message) => UnityEngine.Debug.Log("<color=red>[NETHOSTFIRE]</color> " + Message);
    }

    partial class Nethostfire : Exception{
        public Nethostfire (string message, object? instance = null) : base(message){
            WriteLog(message, true, false);
        }
    }
}