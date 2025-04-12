// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using Nethostfire;
using Nethostfire.MySQL;
using Nethostfire.UDP;

internal class Program{
    static Server Server = new();
    static Client Client = new();
    static MySQL MySQL = new();
    private static void Main(string[] args){
        //TestJson();
        //TestMySQL();
        //Client.Status += OnStatus;
        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }

    static void TestJson(){
        var json = Json.ToJson(Server);
        Server? server = Json.FromJson<Server>(json);
        Console.WriteLine(server == null);
    }

    static void TestMySQL(){
        MySQL.Connect(IPAddress.Parse("127.0.0.1"), 3306, "root", "12345678", "test");
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