// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using UnityEngine;
using static Nethostfire.Utility;

public partial class NethostfireService : MonoBehaviour{

    [RuntimeInitializeOnLoadMethod]
    static void Init() => Dispose();

    // Resolve hight usage cpu
    void Awake() => QualitySettings.vSyncCount = Application.isBatchMode ? 0 : QualitySettings.vSyncCount;

    // Preseve object in unload scene
    void Start() => DontDestroyOnLoad(gameObject);
    
    // Run events in MainThread
    void Update() => ThisMainThread();

    // Disconnect and stop all servers, clients when exit play mode
    void OnDisable() => Dispose();

    // Reset all server and client, too clean memory.
    static void Dispose(){
        // Reset all server in enter play mode
        foreach(var Server in ListServer)
            Server.Dispose();
        
        // Reset all client in enter play mode
        foreach(var Client in ListClient)
            Client.Dispose();

        // Clear actions main thread
        ListRunOnMainThread.Clear();
    }
}