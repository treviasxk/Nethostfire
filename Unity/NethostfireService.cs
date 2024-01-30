// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using UnityEngine;
using Nethostfire;

public partial class NethostfireService : MonoBehaviour{
    public static List<UDP.Client> ListClient = new();
    public static List<UDP.Server> ListServer = new();

    [RuntimeInitializeOnLoadMethod]
    static void Init(){
        Utility.Quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;

        // Reset all client in enter play mode
        foreach(var Client in ListClient){
            bool ShowDebug = Client.ShowLogDebug;
            Client.ShowLogDebug = false;
            Client.Disconnect();
            Client.OnReceivedBytes = null;
            Client.OnStatus = null;
            Client.ShowLogDebug = ShowDebug;
        }


        // Reset all server in enter play mode
        foreach(var Server in ListServer){
            bool ShowDebug = Server.ShowLogDebug;
            Server.ShowLogDebug = false;
            Server.Stop();
            Server.OnReceivedBytes = null;
            Server.OnStatus = null;
            Server.ShowLogDebug = ShowDebug;
        }
    }

    // Disconnect and stop all servers, clients when exit play mode
    static void OnQuitting(){
        Utility.Quitting = true;
        foreach(var Client in ListClient)
            Client.Disconnect();
        foreach(var Server in ListServer)
            Server.Stop();
    }

    // Resolve hight usage cpu
    void Awake() => QualitySettings.vSyncCount = Application.isBatchMode ? 0 : QualitySettings.vSyncCount;

    void Start() => DontDestroyOnLoad(gameObject);
    
    // Run events in MainThread
    void Update() => Utility.ThisMainThread();
}