// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using Nethostfire.UDP;

internal class Program{
    static Server Server = new();
    static Client Client = new();
    private static void Main(string[] args){
        Client.Status += OnStatus;
        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }

    private static void OnStatus(object? sender, SessionStatusEventArgs e){
        if(e.Status == SessionStatus.Connected){
            new Thread(()=>{
            while(true){
                Client.Send("Hello World!", 0, TypeEncrypt.None, TypeShipping.WithoutPacketLossEnqueue);
                Thread.Sleep(100);
            }
            }).Start();
            //Client.Disconnect();
        }
    }
}