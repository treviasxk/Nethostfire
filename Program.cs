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
        Server.OnReceivedNewDataClient += OnReceivedNewDataClient;
        Client.OnReceivedNewDataServer += OnReceivedNewDataServer;
        Client.OnClientStatusConnection += OnClientStatusConnection;
        Menu();
    }

    static void Menu(){
        Console.Title = "Nethostfire";
        Console.WriteLine(" ============== NETHOSTFIRE ==============");
        Console.WriteLine("  Social Networks:               treviasxk");
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
                Server.Start(new IPEndPoint(IPAddress.Any, 25000));
                Console.ReadKey();
            break;
            case "2":
                Client.Connect(new IPEndPoint(IPAddress.Parse("144.22.219.177"), 25000));
                Console.ReadKey();
            break;
            case "3":
                Server.Start(new IPEndPoint(IPAddress.Any, 25000));
                Client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000));
                Console.ReadKey();
            break;
            case "4":
                Client.DisconnectServer();
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
            Client.SendBytes(_text, 11, TypeShipping.AES);
        }
    }

    static void OnReceivedNewDataServer(byte[] _byte, int _groupID){
        Console.Title = "Client - (Ping: " + Client.Ping + " Lost Packets: " + Client.LostPackets + " Packets Per Seconds: " + Client.PacketsPerSeconds + " - Packets Bytes Received: " + Client.PacketsBytesReceived + " - Packets Bytes Sent: " + Client.PacketsBytesSent + ")";
        Console.WriteLine("[CLIENT] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
        //Client.SendBytes(_byte, _groupID);
    }
    
    //========================= Events Server =========================
    static void OnReceivedNewDataClient(byte[] _byte, int _groupID, DataClient _dataClient){
        Console.Title = "Server - (Status: " + Server.Status + " - Lost Packets: " + Server.LostPackets + " - Packets Per Seconds: " + Server.PacketsPerSeconds + " - Packets Bytes Received: " + Server.PacketsBytesReceived + " - Packets Bytes Sent: " + Server.PacketsBytesSent + ")";
        Console.WriteLine("[SERVER] GroupID: {0} - Message: {1} | Length: {2}", _groupID, Encoding.ASCII.GetString(_byte), _byte.Length);
        Server.SendBytes(_byte, _groupID, _dataClient, TypeShipping.AES);
    }
}
