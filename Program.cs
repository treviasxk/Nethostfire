// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Reflection;
using System.Text;
using Nethostfire;

class Program {
    static void Main(string[] args){
        Server.OnConnectedClient += OnConnectedClient;
        Server.OnDisconnectedClient += OnDisconnectedClient;
        Server.OnReceivedNewDataClient += OnReceivedNewDataClient;
        Server.OnServerStatusConnection += OnServerStatusConnection;
        Client.OnReceivedNewDataServer += OnReceivedNewDataServer;
        Client.OnClientStatusConnection += OnClientStatusConnection;
        Menu();
    }

    static void Menu(){
        Console.Title = "Nethostfire";
        Console.WriteLine(" ============== NETHOSTFIRE ==============");
        Console.WriteLine("  REDES SOCIAIS:               treviasxk");
        Console.WriteLine("  VERSÃO:                      {0}", Assembly.GetExecutingAssembly().GetName().Version);
        Console.WriteLine("  LICENÇA:                     GPL-3.0");
        Console.WriteLine(" =========================================");
        Console.WriteLine(" 1 - Server");
        Console.WriteLine(" 2 - Client");
        Console.WriteLine(" 3 - Sair");
        string op = Console.ReadLine();
        Console.Clear();
        switch(op){
            case "1":
                Server.Start(new IPEndPoint(IPAddress.Any, 25000));
                Console.ReadKey();
            break;
            case "2":
                Client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000));
                Console.ReadKey();
            break;
            case "3":
                Client.DisconnectServer();
            break;
            default:
                Menu();
            break;
        }
    }

    //========================= Events Client =========================
    static void OnClientStatusConnection(ClientStatusConnection _status){
        Console.WriteLine("[STATUS] {0}", _status);
        if(_status == ClientStatusConnection.Connected){
            var _text = Encoding.UTF8.GetBytes("Hello world");
            Client.SendBytes(_text, _text.GetHashCode(), true);
        }
    }
    static void OnReceivedNewDataServer(byte[] _byte, int _hashCode){
        //Console.Title = "Client - (Ping: " + Client.Ping + " Packets Per Seconds: " + Client.PacketsPerSeconds + " - Packets Size Received: " + Client.PacketsSizeReceived + " - Packets Size Sent: " + Client.PacketsSizeSent + ")";
        //Console.WriteLine("[RECEIVED] {0} - {1}", _hashCode, Encoding.UTF8.GetString(_byte));
        Client.SendBytes(_byte, _hashCode);
    }
    
    //========================= Events Server =========================
    static void OnConnectedClient(DataClient _dataClient){
        Console.WriteLine("[CLIENT] {0} conectou no servidor.", _dataClient.IP);
    }
    static void OnDisconnectedClient(DataClient _dataClient){
        Console.WriteLine("[CLIENT] {0} desconectou do servidor.", _dataClient.IP);
    }

    static void OnReceivedNewDataClient(byte[] _byte, int _hashCode, DataClient _dataClient){
        Console.Title = "Server - (Ping: " + _dataClient.Ping + " Packets Per Seconds: " + Server.PacketsPerSeconds + " - Packets Size Received: " + Server.PacketsSizeReceived + " - Packets Size Sent: " + Server.PacketsSizeSent + ")";
        //Console.WriteLine("[RECEIVED] {0} - {1}", _hashCode, Encoding.UTF8.GetString(_byte));
        Server.SendBytes(_byte, _hashCode, _dataClient);
    }
    static void OnServerStatusConnection(ServerStatusConnection _status){
        if(_status == ServerStatusConnection.Running)
            Console.WriteLine("[SERVER] Servidor iniciado e hospedado na porta: {0}", 25000);
        else
            Console.WriteLine("[STATUS] " + _status);
    }
}