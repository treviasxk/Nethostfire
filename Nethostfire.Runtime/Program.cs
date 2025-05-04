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

    public class Message{
        public string? text { get; set; }
    }
    private static void Main(string[] args){
        TestJson();
        TestMySQL();
        //Client.Status += OnStatus;

        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }

    static void TestJson(){
        var message = new Message(){text = "Hello World!"};
        var json = Json.ToJson(message);
        var obj = Json.FromJson<Message>(json);
        Console.WriteLine(obj?.text);

        message = new Message(){text = "Hello World! 2"};
        json = Json.ToJson(message);
        obj = Json.FromJson<Message>(json);
        Console.WriteLine(obj?.text);
    }

    static void TestMySQL(){
        //MySQL.StateChanged += OnState;
        MySQL.Connect(IPAddress.Loopback, 3306, "user", "root", "12345678");
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