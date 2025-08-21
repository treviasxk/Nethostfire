// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Text;

namespace TreviasXk {    
    public partial class Nethostfire
    {
        static StreamWriter? fileLog;
        static int InstanceID = 0;
        public static bool SaveLogs { get; set; } = true;
        public static bool SupressError { get; set; }
        public static bool IsUnityBatchMode { get; internal set; }
        public static bool IsRunningInUnity { get; internal set; }

        internal static ushort GetPing(long Timer)
        {
            var ping = ((DateTime.Now.Ticks - Timer) / TimeSpan.TicksPerMillisecond) - 1000;
            return Convert.ToUInt16(ping >= ushort.MinValue && ping <= ushort.MaxValue ? ping : 0);
        }

        internal static void LoadUnity()
        {
            // Resolve high usage cpu in BatchMode Unity
            IsUnityBatchMode = UnityEngine.Application.isBatchMode;
            UnityEngine.QualitySettings.vSyncCount = IsUnityBatchMode ? 0 : UnityEngine.QualitySettings.vSyncCount;
        }

        internal static void StartUnity()
        {
            try
            {
                LoadUnity();
                IsRunningInUnity = true;
            }
            catch { }
        }

        internal static void InvokeEvent(Action action, object? instance)
        {
            Parallel.Invoke(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    if (SupressError)
                        WriteLog(ex, instance, false);
                    else
                        throw new Nethostfire(ex, instance);
                }
            });
        }


        public static void WriteLog(Exception exception, object? instance = null, bool showLog = true) => WriteLog(exception.Message + exception.StackTrace, exception, showLog);
        public static void WriteLog(object? message, object? instance = null, bool showLog = true) => WriteLog(message?.ToString() ?? "", instance, showLog);
        public static void WriteLog(string message, object? instance = null, bool showLog = true)
        {
            string InstanceName = instance == null ? "" : $"[{instance.GetType().Name.ToUpper()}] ";

            if (SaveLogs)
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
                            fileLog = new StreamWriter($"logs/{AppDomain.CurrentDomain.FriendlyName}-{InstanceID}-{filename}", true, Encoding.UTF8) { AutoFlush = true };
                            break;
                        }
                        catch
                        {
                            InstanceID++;
                        }
                }

                fileLog.WriteLine(text);
            }

            if (showLog)
                if (IsRunningInUnity && !IsUnityBatchMode)
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
        public Nethostfire(Exception exception, object? instance = null) => WriteLog(exception.Message + exception.StackTrace, instance == null ? exception : instance);
        public Nethostfire(string message) => WriteLog(message);
    }
}