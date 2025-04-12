// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Nethostfire.UDP;

namespace Nethostfire {
    public enum MySQLStatus {
        Connected,
        Connecting,
        Disconnected,
    }

    class System{
        public static ConcurrentQueue<Action> ListRunOnMainThread = new();
        public static Process Process = Process.GetCurrentProcess();
        public static HashSet<Client> ListClient = new();
        public static HashSet<Server> ListServer = new();
        static StreamWriter? fileLog;
        static int currentInstance = 0;
        public static bool SaveLog = true;
        public static bool UnityBatchMode = false, RunningInUnity = false;

        public static ushort GetPing(long Timer){
            var ping = ((DateTime.Now.Ticks - Timer) / TimeSpan.TicksPerMillisecond) - 1000;
            return Convert.ToUInt16(ping >= ushort.MinValue && ping <= ushort.MaxValue ? ping : 0);
        }

        public static void RunOnMainThread(Action action){
            if(RunningInUnity)
                ListRunOnMainThread.Enqueue(action);
            else
                Parallel.Invoke(() => action?.Invoke());
        }

        public static void ThisMainThread(){
            while(ListRunOnMainThread.TryDequeue(out var action))
                if(UnityBatchMode){
                    Parallel.Invoke(() => action?.Invoke());
                }else
                    action?.Invoke();
        }

        public static void LoadUnity(Client? client = null, Server? server = null){
            UnityBatchMode = UnityEngine.Application.isBatchMode;

            if(!UnityEngine.GameObject.Find("[Nethostfire]")){
                if(UnityBatchMode)
                    Console.Clear();
                new UnityEngine.GameObject("[Nethostfire]").AddComponent<Nesthostfire>().hideFlags = UnityEngine.HideFlags.HideInHierarchy;
            }

            if(client != null && !ListClient.Contains(client))
                ListClient.Add(client);

            if(server != null && !ListServer.Contains(server))
                ListServer.Add(server);
        }

        public static void StartUnity(Client? client = null, Server? server = null){
            try{
                LoadUnity(client, server);
                RunningInUnity = true;
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    if(UnityBatchMode || !RunningInUnity)
                        Process.PriorityClass = ProcessPriorityClass.High;
            }catch{}
        }


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
            
            if(showLog)
            if(RunningInUnity && !UnityBatchMode)
                ShowUnityLog(message);
            else{
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

    class Nethostfire : Exception{
        public Nethostfire (string message, object? instance = null) : base(message){
            System.WriteLog(message, true, false);
        }
    }
}