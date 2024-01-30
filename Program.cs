using System.Net;
using System.Text;
using Nethostfire;

internal class Program{
    static UDP.Server Server = new();
    static UDP.Client Client = new();
    private static void Main(string[] args){
        Server.OnReceivedBytes = OnReceivedBytesClient;
        Server.OnConnected = OnConnected;
        Client.OnReceivedBytes = OnReceivedBytesServer;
        Client.OnStatus = OnStatusClient;
        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Console.ReadLine();
    }

    private static void OnConnected(IPEndPoint point){

    }

    private static void OnStatusClient(ClientStatus status){
        if(status == ClientStatus.Connected){
            Client.Send(Encoding.UTF8.GetBytes("Hello World!"), 0, TypeEncrypt.RSA, TypeShipping.Enqueue);
            Client.Send(Encoding.UTF8.GetBytes("Hello World!2"), 0, TypeEncrypt.RSA, TypeShipping.Enqueue);
        }
    }

    private static void OnReceivedBytesClient(byte[] arg1, int arg2, IPEndPoint point){
        Console.WriteLine("[SERVER] " + Encoding.UTF8.GetString(arg1) + " groupID: " + arg2 + " IP: " + point);
        //Server.Send(arg1, arg2, point, TypeEncrypt.RSA, TypeShipping.Enqueue);
    }

    private static void OnReceivedBytesServer(byte[] arg1, int arg2){
        Console.WriteLine("[CLIENT] " + Encoding.UTF8.GetString(arg1) + " groupID: " + arg2);
    }
}