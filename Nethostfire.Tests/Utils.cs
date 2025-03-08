using System.Net;
using System.Text;

namespace Nethostfire.Tests;
public class Utils{
    public static bool SendServerPacket(TypeEncrypt typeEncrypt){
        bool result = false;
        var message = "Hello World!";
        var client = new UDP.Client();
        client.OnReceivedBytes += (bytes, groupID) =>{
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

        server.Stop();
        client.Disconnect();
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
        server.OnReceivedBytes += (bytes, groupID, ip) =>{
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

        server.Stop();
        client.Disconnect();
        return result;
    }
}