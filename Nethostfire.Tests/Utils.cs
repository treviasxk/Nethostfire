using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Nethostfire.Tests;
public class Utils{
    public static bool TestOffline(bool clientoff = false){
        var result = false;
        var client = new UDP.Client();
        var server = new UDP.Server();

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

        client.OnStatus = (status) =>{
            if(status == SessionStatus.Connected)
                result = true;
        };

        server.OnConnected = (ip) =>{
            result = result && server.Sessions.GetStatus(ip) == SessionStatus.Connected;
        };

        Thread.Sleep(5000);
        server.Stop();
        client.Disconnect();
        return result;
    }

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

    public static bool TestLimitGroupIdPPSClient(bool group = false){
        var result = false;
        int x = 0;
        var client = new UDP.Client();
        var server = new UDP.Server();
        ushort pps = 5;
        Session session = new();
        
        client.OnStatus = (status) =>{
            if(status == SessionStatus.Connected && group){
                for(int i = 0; i < pps * 2; i++){
                    client.Send("Hello", 10);
                    Thread.Sleep(1000 / (pps * 2));
                }
            }
        };

        if(!group){
            client.SetReceiveLimitGroupPPS(10, pps);
            client.OnReceivedBytes = (bytes, groupID) =>{
                x++;
            };
        }
        else{
            server.SetReceiveLimitGroupPPS(10, pps);
            server.OnReceivedBytes = (bytes, groupID, ip) =>{
                x++;
            };
        }

        server.OnConnected = (ip) =>{
            server.Sessions.TryGetValue(ip, out session);
            if(group)
                server.SetReceiveLimitGroupPPS(10, pps);
            else{
                for(int i = 0; i < pps * 3; i++){
                    server.Send("Hello", 10, ref ip);
                    Thread.Sleep(1000 / (pps * 3));
                }
            }
        };

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(4000);

        result = x == pps;

        server.Dispose();
        client.Dispose();
        return result;
    }

    public static bool TestLimitSendPPSClient(){
        var result = false;
        int x = 0;
        var client = new UDP.Client();
        var server = new UDP.Server();
        ushort pps = 5;

        client.SetSendLimitGroupPPS(10, pps);

        client.OnStatus = (status) =>{
            if(status == SessionStatus.Connected){
                for(int i = 0; i < pps * 2; i++){
                    client.Send("Hello", 10);
                    Thread.Sleep(1000 / (pps * 2));
                }
            }
        };

        server.OnReceivedBytes = (bytes, groupID, ip) =>{
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