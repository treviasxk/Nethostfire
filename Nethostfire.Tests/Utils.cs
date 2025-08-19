// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Nethostfire.UDP;

namespace Nethostfire.Tests;
public class Utils{
    public static bool TestOffline(bool clientoff = false){
        var result = false;
        var client = new Client();
        var server = new Server();

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(3000);

        if(clientoff)
            client.Disconnect();
        else
            server.Stop();
            
        Thread.Sleep(client.ConnectTimeout + 1100);

        if(clientoff)
            client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        else
            server.Start(IPAddress.Any, 25000);

        client.Status += (ob, e) =>{
            if(e.Status == SessionStatus.Connected)
                result = true;
        };

        server.Connected += (ob, e) =>{
            result = result && server.Sessions[e.IP].Status == SessionStatus.Connected;
        };

        Thread.Sleep(5000);
        server.Stop();
        client.Disconnect();
        return result;
    }

    public static bool SendServerPacket(TypeEncrypt typeEncrypt){
        bool result = false;
        var message = "Hello World!";
        var client = new Client();
        client.DataReceived += (ob, e) =>{
            if(typeEncrypt == TypeEncrypt.OnlyCompress || typeEncrypt == TypeEncrypt.OnlyBase64){
                if(typeEncrypt == TypeEncrypt.OnlyCompress){
                    var text = Encoding.UTF8.GetString(DataSecurity.Decompress(e.Bytes));
                    result = text == message;
                }
                if(typeEncrypt == TypeEncrypt.OnlyBase64){
                    var text = Encoding.UTF8.GetString(DataSecurity.DecryptBase64(e.Bytes));
                    result = text == message;
                }
            }else{
                result = Encoding.UTF8.GetString(e.Bytes) == message && e.GroupID == 100;
            }
        };

        var server = new Server();
        server.Connected += (ob, e) =>{
            server.Send(Encoding.UTF8.GetBytes(message), 100, e.IP, typeEncrypt);
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        server.Dispose();
        client.Dispose();
        return result;
    }

    public static bool SendClientPacket(TypeEncrypt typeEncrypt){
        bool result = false;
        var message = "Hello World!";
        var client = new Client();
        client.Status += (ob, e)=>{
            if(e.Status == SessionStatus.Connected)
                client.Send(Encoding.UTF8.GetBytes(message), 100, typeEncrypt);
        };

        var server = new Server();
        server.DataReceived += (ob, e) =>{
            if(typeEncrypt == TypeEncrypt.OnlyCompress || typeEncrypt == TypeEncrypt.OnlyBase64){
                if(typeEncrypt == TypeEncrypt.OnlyCompress){
                    var text = Encoding.UTF8.GetString(DataSecurity.Decompress(e.Bytes));
                    result = text == message;
                }
                if(typeEncrypt == TypeEncrypt.OnlyBase64){
                    var text = Encoding.UTF8.GetString(DataSecurity.DecryptBase64(e.Bytes));
                    result = text == message;
                }
            }else{
                result = Encoding.UTF8.GetString(e.Bytes) == message && e.GroupID == 100;
            }
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        server.Dispose();
        client.Dispose();
        return result;
    }

    public struct Message{
        public string? text { get; set; }
    }
    public static bool SendClientPacket(bool nulle = true){
        Message message = new(){text = "Hello World!"};
        bool result = false;
        var client = new Client();
        client.Status += (ob, e)=>{
            if(e.Status == SessionStatus.Connected)
                client.Send(nulle ? null : JsonSerializer.SerializeToUtf8Bytes(message), 100);
        };

        var server = new Server();
        server.DataReceived += (ob, e) =>{
            if(nulle)
                result = e.Bytes.Length == 0 && e.GroupID == 100;
            else
                result = JsonSerializer.Deserialize<Message>(e.Bytes).Equals(message) && e.GroupID == 100;
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        server.Stop();
        client.Disconnect();
        return result;
    }


    public static bool SendServerPacket(bool nulle = true){
        Message message = new(){text = "Hello World!"};
        bool result = false;
        var client = new Client();
        var server = new Server();

        server.Connected += (ob, e) =>{
            server.Send(nulle ? null : JsonSerializer.SerializeToUtf8Bytes(message), 100, e.IP);
        };

        client.DataReceived += (ob, e) =>{
            if(nulle)
                result = e.Bytes.Length == 0 && e.GroupID == 100;
            else
                result = JsonSerializer.Deserialize<Message>(e.Bytes).Equals(message) && e.GroupID == 100;
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        
        Thread.Sleep(5000);
        server.Dispose();
        client.Dispose();
        return result;
    }

   public static bool SendServerPacketGroup(bool nulle = true){
        Message message = new(){text = "Hello World!"};
        bool result = false, result2 = false;
        var client = new Client();
        var client2 = new Client();
        var server = new Server();

        ConcurrentQueue<IPEndPoint> ips = new();

        server.Connected += (ob, e) =>{
            ips.Enqueue(e.IP);
            if(ips.Count == 2)
                server.SendGroup(nulle ? null : JsonSerializer.SerializeToUtf8Bytes(message), 100, ips);
        };

        client.DataReceived += (ob, e) =>{
            if(nulle)
                result = e.Bytes.Length == 0 && e.GroupID == 100;
            else
                result = JsonSerializer.Deserialize<Message>(e.Bytes).Equals(message) && e.GroupID == 100;
        };
        client2.DataReceived += (ob, e) =>{
            if(nulle)
                result2 = e.Bytes.Length == 0 && e.GroupID == 100;
            else
                result2 = JsonSerializer.Deserialize<Message>(e.Bytes).Equals(message) && e.GroupID == 100;
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        client2.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);
        server.Dispose();
        client.Dispose();
        client2.Dispose();
        return result && result2;
    }

    public static bool TestLimitGroupIdPPSClient(bool isClient = false){
        var result = false;
        int x = 0;
        var client = new Client();
        var server = new Server();
        int pps = 5;
        Session? session = new();
        
        client.Status += (ob, e) =>{
            if(e.Status == SessionStatus.Connected && isClient){
                for(int i = 0; i < pps * 3; i++){
                    client.Send("Hello", 10);
                    Thread.Sleep(1000 / (pps * 3));
                }
            }
        };

        if(!isClient){
            client.SetReceiveLimitGroupPPS(10, pps);
            client.DataReceived += (ob, e) =>{
                x++;
            };
        }
        else{
            server.SetReceiveLimitGroupPPS(10, pps);
            server.DataReceived += (ob, e) =>{
                x++;
            };
        }

        server.Connected += (ob, e) =>{
            server.Sessions.TryGetValue(e.IP, out session);
            if(isClient)
                server.SetReceiveLimitGroupPPS(10, pps);
            else{
                for(int i = 0; i < pps * 3; i++){
                    server.Send("Hello", 10, e.IP);
                    Thread.Sleep(1000 / (pps * 3));
                }
            }
        };

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        result = x <= pps;

        server.Dispose();
        client.Dispose();
        return result;
    }

    public static bool TestLimitSendPPSClient(){
        var result = false;
        int x = 0;
        var client = new Client();
        var server = new Server();
        ushort pps = 5;

        client.SetSendLimitGroupPPS(10, pps);

        client.Status += (ob, e) =>{
            if(e.Status == SessionStatus.Connected){
                for(int i = 0; i < pps * 2; i++){
                    client.Send("Hello", 10);
                    Thread.Sleep(1000 / (pps * 2));
                }
            }
        };

        server.DataReceived += (ob, e) =>{
            x++;
        };

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(4000);

        result = x == pps;

        server.Dispose();
        client.Dispose();
        return result;
    }
}