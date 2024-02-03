// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using UnityEngine;
using Nethostfire;

public partial class NethostfireService : MonoBehaviour{

    [RuntimeInitializeOnLoadMethod]
    static void Init(){
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
        Dispose();
    }

    static void Dispose(){
        // Reset all server in enter play mode
        foreach(var Server in Utility.ListServer)
            Server.Dispose();
        
        // Reset all client in enter play mode
        foreach(var Client in Utility.ListClient)
            Client.Dispose();

        // Clear actions main thread
        Utility.ListRunOnMainThread.Clear();
    }

    // Resolve hight usage cpu
    void Awake() => QualitySettings.vSyncCount = Application.isBatchMode ? 0 : QualitySettings.vSyncCount;

    void Start() => DontDestroyOnLoad(gameObject);
    
    // Run events in MainThread
    void Update() => Utility.ThisMainThread();

    // Disconnect and stop all servers, clients when exit play mode
    static void OnQuitting() => Dispose();
}