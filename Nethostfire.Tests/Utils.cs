using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Nethostfire.Tests;
public class Utils{
    public static bool SendServerPacket(TypeEncrypt typeEncrypt){
        bool result = false;
        var message = "Hello World!";
        var client = new UDP.Client();
        client.OnReceivedBytes = (bytes, groupID) =>{
            if(typeEncrypt == TypeEncrypt.OnlyCompress || typeEncrypt == TypeEncrypt.OnlyBase64){
                if(typeEncrypt == TypeEncrypt.OnlyCompress){
                    var text = Encoding.UTF8.GetString(DataSecurity.Decompress(bytes));
                    result = text == message;
                }
                if(typeEncrypt == TypeEncrypt.OnlyBase64){
                    var text = Encoding.UTF8.GetString(DataSecurity.DecryptBase64(bytes));
                    result = text == message;
                }
            }else{
                result = Encoding.UTF8.GetString(bytes) == message && groupID == 100;
            }
        };

        var server = new UDP.Server();
        server.OnConnected = (ip) =>{
            server.Send(Encoding.UTF8.GetBytes(message), 100, ip, typeEncrypt);
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
        var client = new UDP.Client();
        client.OnStatus = (status)=>{
            if(status == SessionStatus.Connected)
                client.Send(Encoding.UTF8.GetBytes(message), 100, typeEncrypt);
        };

        var server = new UDP.Server();
        server.OnReceivedBytes = (bytes, groupID, ip) =>{
            if(typeEncrypt == TypeEncrypt.OnlyCompress || typeEncrypt == TypeEncrypt.OnlyBase64){
                if(typeEncrypt == TypeEncrypt.OnlyCompress){
                    var text = Encoding.UTF8.GetString(DataSecurity.Decompress(bytes));
                    result = text == message;
                }
                if(typeEncrypt == TypeEncrypt.OnlyBase64){
                    var text = Encoding.UTF8.GetString(DataSecurity.DecryptBase64(bytes));
                    result = text == message;
                }
            }else{
                result = Encoding.UTF8.GetString(bytes) == message && groupID == 100;
            }
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        server.Dispose();
        client.Dispose();
        return result;
    }

    struct Teste {
        public string msg;
    }
    public static bool SendClientPacket(bool nulle = true){
        Teste teste = new(){msg = "Hello World!"};
        bool result = false;
        var client = new UDP.Client();
        client.OnStatus = (status)=>{
            if(status == SessionStatus.Connected)
                client.Send(nulle ? null : Json.GetBytes(teste), 100);
        };

        var server = new UDP.Server();
        server.OnReceivedBytes = (bytes, groupID, ip) =>{
            if(nulle)
                result = bytes.Length == 0 && groupID == 100;
            else
                result = Json.FromJson<Teste>(bytes).Equals(teste) && groupID == 100;
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        server.Stop();
        client.Disconnect();
        return result;
    }


    public static bool SendServerPacket(bool nulle = true){
        Teste teste = new(){msg = "Hello World!"};
        bool result = false;
        var client = new UDP.Client();
        var server = new UDP.Server();

        server.OnConnected = (ip) =>{
            server.Send(nulle ? null : Json.GetBytes(teste), 100, ip);
        };

        client.OnReceivedBytes = (bytes, groupID) =>{
            if(nulle)
                result = bytes.Length == 0 && groupID == 100;
            else
                result = Json.FromJson<Teste>(bytes).Equals(teste) && groupID == 100;
        };
        
        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        
        Thread.Sleep(5000);
        server.Dispose();
        client.Dispose();
        return result;
    }

   public static bool SendServerPacketGroup(bool nulle = true){
        Teste teste = new(){msg = "Hello World!"};
        bool result = false, result2 = false;
        var client = new UDP.Client();
        var client2 = new UDP.Client();
        var server = new UDP.Server();

        ConcurrentQueue<IPEndPoint> ips = new();

        server.OnConnected = (ip) =>{
            ips.Enqueue(ip);
            if(ips.Count == 2)
                server.SendGroup(nulle ? null : Json.GetBytes(teste), 100, ref ips);
        };


        client.OnReceivedBytes = (bytes, groupID) =>{
            if(nulle)
                result = bytes.Length == 0 && groupID == 100;
            else
                result = Json.FromJson<Teste>(bytes).Equals(teste) && groupID == 100;
        };
        client2.OnReceivedBytes = (bytes, groupID) =>{
            if(nulle)
                result2 = bytes.Length == 0 && groupID == 100;
            else
                result2 = Json.FromJson<Teste>(bytes).Equals(teste) && groupID == 100;
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
}