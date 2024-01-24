// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com
// Documentation:       https://github.com/treviasxk/Nethostfire/blob/master/UDP/README.md

using UnityEngine;
using Nethostfire;

public class NethostfireService : MonoBehaviour{
    [Header("Nethostfire")]
    GUIStyle TextStyle;
    Material mat;
    List<ChartGraph> ListGraph;
    ChartGraph Latency, FPS;
    float tmp, count;


    [RuntimeInitializeOnLoadMethod]
    static void Init(){
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
        bool ShowDebug = Utility.ShowDebugConsole;
        Utility.Quitting = false;
        Utility.ShowDebugConsole = false;
        UDP.Client.DisconnectServer();
        UDP.Server.Stop();
        UDP.Client.OnClientStatus = null;
        UDP.Client.OnReceivedBytes = null;
        UDP.Server.OnConnectedClient = null;
        UDP.Server.OnDisconnectedClient = null;
        UDP.Server.OnReceivedBytes = null;
        UDP.Server.OnServerStatus = null;
        Utility.ShowDebugConsole = ShowDebug;
        Utility.listHoldConnectionQueueClient.Clear();
        Utility.listHoldConnectionClient.Clear();
        Utility.ListRunOnMainThread.Clear();
    }

    static void OnQuitting(){
        Utility.Quitting = true;
        UDP.Client.DisconnectServer();
        UDP.Server.Stop();
    }

    void Awake(){
        if(Utility.UnityBatchMode){
            QualitySettings.vSyncCount = 0;
        }
    }

    void Start(){
        DontDestroyOnLoad(gameObject);
        if(!Application.isBatchMode){
            ListGraph  = new();
            TextStyle = new(){fontSize = 10, alignment = TextAnchor.UpperLeft, padding = new RectOffset(3, 0, 3, 0)};
            mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            Latency = CreateGraph(300, "PING", new Rect(170, 4, 100, 37));
            FPS = CreateGraph(500, "FPS", new Rect(170, 46, 100, 37));
        }
    }

    void Update() {
        if(tmp + 1 < Time.time){ 
            tmp = Time.time;
            if(UDP.Server.ShowUnityNetworkStatistics && UDP.Server.Status == ServerStatusConnection.Running)
                Utility.ShowLog("FPS: " + count + " PPS: " + UDP.Server.PacketsPerSeconds + " LostPackets: " + UDP.Server.LostPackets + " BufferMainThread: " + Utility.ListRunOnMainThread.Count + " PacketsSizeReceived: " + UDP.Server.PacketsBytesReceived + " PacketsSizeSent: " + UDP.Server.PacketsBytesSent);
            if(!Utility.UnityBatchMode){
                AddValueGraph(count, FPS);
                AddValueGraph(UDP.Client.Ping, Latency);
            }
            count = 0;
        }
        count++;
        Utility.ThisMainThread();
    }

    void OnGUI(){
        if(!Application.isBatchMode && UDP.Client.ShowUnityNetworkStatistics){
            GUILayout.Label("<color=white><b>Nethostfire " + Utility.GetVersion + "</b></color>", TextStyle);
            GUILayout.Label("<color=white>Status: " + UDP.Client.Status + "</color>", TextStyle);
            GUILayout.Label("<color=white>Lost Packets: " + UDP.Client.LostPackets + "</color>", TextStyle);
            GUILayout.Label("<color=white>Packets Peer Seconds: " + UDP.Client.PacketsPerSeconds + "</color>", TextStyle);
            GUILayout.Label("<color=white>Packets Size Received: " + UDP.Client.PacketsBytesReceived + "</color>", TextStyle);
            GUILayout.Label("<color=white>Packets Size Sent: " + UDP.Client.PacketsBytesSent + "</color>", TextStyle);
            GUILayout.Label("<color=white>Buffer Main Thread: " + Utility.ListRunOnMainThread.Count + "</color>", TextStyle);
            GUILayout.Label("<color=white>Connect Time Out: " + UDP.Client.ConnectTimeOut + "</color>", TextStyle);
            GUILayout.Label("<color=white>Receive And Send Time Out: " + UDP.Client.ReceiveAndSendTimeOut + "</color>", TextStyle);
            for(int i = 0; i < ListGraph.Count; i++)
                ShowGraph(ListGraph[i]);
        }
    }


    class ChartGraph {
        public Rect windowRect;
        public string Name;
        public float value;
        public float maxValue;
        public List<float> values = new();
    }

    void AddValueGraph(float value, ChartGraph graph){
        if(UDP.Client.ShowUnityNetworkStatistics){
            graph.value = value;
            float b = graph.windowRect.height / graph.maxValue;
            value = b*value;
            if(value > graph.windowRect.height)
                value = graph.windowRect.height;
            if(value < 0)
                value = 0;
            graph.values.Add(value);
        }
    }

    ChartGraph CreateGraph(int maxValue, string name, Rect windowRect){
        ChartGraph graph = new()
        {
            windowRect = windowRect,
            maxValue = maxValue,
            Name = name
        };
        ListGraph.Add(graph);
        return graph;
    }

    void ShowGraph(ChartGraph graph){
        if(graph.values.Count > 0 && Event.current.type == EventType.Repaint){
            while(graph.windowRect.width < graph.values.Count)
                graph.values.RemoveAt(0);

            GL.PushMatrix();

            GL.Clear(true, false, Color.black);
            mat.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Color(new Color(1f,1f,1f,0.1f));
            for(int i = 0; i < 4; i++){
                var xs = graph.windowRect.height / 4;
                GL.Vertex3(graph.windowRect.x, graph.windowRect.y + xs * i, 0);
                GL.Vertex3(graph.windowRect.x + graph.windowRect.width, graph.windowRect.y + xs * i, 0);
            }
            for(int i = 0; i < 4; i++){
                var xs = graph.windowRect.width / 4;
                GL.Vertex3(graph.windowRect.x + xs * i, graph.windowRect.y, 0);
                GL.Vertex3(graph.windowRect.x + xs * i, graph.windowRect.y + graph.windowRect.height, 0);
            }
            GL.End();

            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.0f, 0.0f, 0.0f,0.2f));
            GL.Vertex3(graph.windowRect.x, graph.windowRect.y, 0);
            GL.Vertex3(graph.windowRect.width + graph.windowRect.x, graph.windowRect.y, 0);
            GL.Vertex3(graph.windowRect.width + graph.windowRect.x, graph.windowRect.height + graph.windowRect.y, 0);
            GL.Vertex3(graph.windowRect.x, graph.windowRect.height + graph.windowRect.y, 0);
            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(new Color(1f,1f,1f,1f));


            float max = graph.values.Max() + 1;
            float min = graph.values.Min();

            for(int i = 0; i < graph.values.Count; i++){
                if(i > 1){
                    float y2 = Mathf.InverseLerp(max, min, graph.values[i]) * graph.windowRect.height + graph.windowRect.y;
                    float y1 = Mathf.InverseLerp(max, min, graph.values[i - 1]) * graph.windowRect.height + graph.windowRect.y;
                    GL.Vertex3(i + graph.windowRect.x, y2, 0);
                    GL.Vertex3((i - 1) + graph.windowRect.x, y1, 0);
                }
            }

            GL.End();
            GL.PopMatrix();
            var style = new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                fontSize = 10
            };
            GUI.Label(new Rect(graph.windowRect.x,graph.windowRect.y,graph.windowRect.width,graph.windowRect.height),"<color=white>"+ graph.Name + ": " + graph.value + "</color>", style);
        }
    }
}