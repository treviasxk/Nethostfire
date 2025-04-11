using System.Net;
using Nethostfire;

internal class Program{
    static UDP.Server Server = new();
    static UDP.Client Client = new();
    private static void Main(string[] args){
        Client.OnStatus += OnStatus;
        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }

    private static void OnStatus(SessionStatus status){
        if(status == SessionStatus.Connected){
            new Thread(()=>{
            while(true){
                Client.Send("Hello World!", 0, TypeEncrypt.None, TypeShipping.WithoutPacketLossEnqueue);
                Thread.Sleep(100);
            }
            }).Start();

        }
         //Client.Disconnect();
        
    }
}