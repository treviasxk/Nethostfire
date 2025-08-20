// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using MessagePack;
using Nethostfire.UDP;

internal class Program{
    static Server Server = new Server();
    static Client Client = new Client();
    [MessagePackObject(AllowPrivate = true)]
    public struct Message
    {
        [Key(0)]
        public string text { get; set; }
    }
    private static void Main(string[] args){
        Client.StatusChanged += OnStatus;
        Server.DataReceived += OnDataReceived;
        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }

    private static void OnDataReceived(object? sender, ServerDataReceivedEventArgs e)
    {
        var message = MessagePackSerializer.Deserialize<Message[]>(e.Bytes);
        Console.WriteLine(message[0].text);
    }

    private static void OnStatus(object? sender, SessionStatusEventArgs e) {
        if (e.Status == SessionStatus.Connected) {
            Client.Dispose();
            new Thread(() =>
            {
                while (true)
                {
                    Message[] messages = [new Message() { text = "HIIIIII" }];
                    Client.Send(MessagePackSerializer.Serialize(messages), 0, TypeEncrypt.None, TypeShipping.WithoutPacketLossEnqueue);
                    Thread.Sleep(100);
                }
            }).Start();
            //Client.Disconnect();
        }
    }
}