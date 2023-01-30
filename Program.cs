// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Net;
using System.Reflection;
using System.Text;
using Nethostfire;

class Program {

    static void Main(string[] args){
        UDpServer.OnReceivedNewDataClient += OnReceivedNewDataClient;
        UDpClient.OnReceivedNewDataServer += OnReceivedNewDataServer;
        UDpClient.OnClientStatusConnection += OnClientStatusConnection;
        Menu();
    }

    static void Menu(){
        Console.Title = Assembly.GetExecutingAssembly().GetName().Name;
        Console.WriteLine(" ============== NETHOSTFIRE ==============");
        Console.WriteLine("  SOCIAL NETWORKS:               treviasxk");
        Console.WriteLine("  VERSION:                       {0}", Assembly.GetExecutingAssembly().GetName().Version);
        Console.WriteLine("  LICENSE:                       GPL-3.0");
        Console.WriteLine(" =========================================");
        Console.WriteLine(" 1 - Server");
        Console.WriteLine(" 2 - Client");
        Console.WriteLine(" 3 - Server & Client");
        Console.WriteLine(" 4 - Sair");
        string op = Console.ReadLine();
        Console.Clear();
        switch(op){
            case "1":
                UDpServer.Start(IPAddress.Any, 25000);
                Console.ReadKey();
            break;
            case "2":
                UDpClient.Connect(IPAddress.Parse("127.0.0.1"), 25000);
                Console.ReadKey();
            break;
            case "3":
                UDpServer.Start(IPAddress.Any, 25000);
                UDpClient.Connect(IPAddress.Parse("127.0.0.1"), 25000);
                Console.ReadKey();
            break;
            case "4":
                
            break;
            default:
                Menu();
            break;
        }
    }

    //========================= Events Client =========================
    static void OnClientStatusConnection(ClientStatusConnection _status){
        if(_status == ClientStatusConnection.Connected){
            var _text =  Encoding.ASCII.GetBytes("Hello world!");
            UDpClient.SendBytes(null, 11, _holdConnection: true);
        }
    }

    static void OnReceivedNewDataServer(byte[] _byte, int _groupID){
        Console.Title = "Client - (Ping: " + UDpClient.Ping + " Lost Packets: " + UDpClient.LostPackets + " - Packets Per Seconds: " + UDpClient.PacketsPerSeconds + " - Packets Bytes Received: " + UDpClient.PacketsBytesReceived + " - Packets Bytes Sent: " + UDpClient.PacketsBytesSent + ")";
        Console.WriteLine("[SERVER] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
        //UDpClient.SendBytes(_byte, _groupID);
    }
    
    //========================= Events Server =========================
    static void OnReceivedNewDataClient(byte[] _byte, int _groupID, DataClient _dataClient){
        Console.Title = "Server - (Status: " + UDpServer.Status + " - Lost Packets: " + UDpServer.LostPackets + " - Packets Per Seconds: " + UDpServer.PacketsPerSeconds + " - Packets Bytes Received: " + UDpServer.PacketsBytesReceived + " - Packets Bytes Sent: " + UDpServer.PacketsBytesSent + ")";
        Console.WriteLine("[CLIENT] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
        UDpServer.SendBytes(null, _groupID, _dataClient, _holdConnection: true);
    }
}
