using System.Net;
using Nethostfire;

internal class Program{
    static UDP.Server Server = new();
    static UDP.Client Client = new();
    private static void Main(string[] args){
        Client.OnStatus += OnStatus;
        Server.Start(IPAddress.Any, 25000);
        //Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }

    private static void OnStatus(SessionStatus status){
        //if(status == SessionStatus.Connected)
         //Client.Disconnect();
    }
}