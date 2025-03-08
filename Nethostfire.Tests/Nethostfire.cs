using System.Net;
using System.Text;
namespace Nethostfire.Tests;

public class Nethostfire{
    [Fact(DisplayName = "Test Send Client Packet")]
    public void TestSendClientPacket() => Assert.True(SendClientPacket(TypeEncrypt.None));

    [Fact(DisplayName = "Test Send Client Packet Compress")]
    public void TestSendClientPacketCompress() => Assert.True(SendClientPacket(TypeEncrypt.Compress));

    [Fact(DisplayName = "Test Send Client Packet RSA")]
    public void TestSendClientPacketRSA() => Assert.True(SendClientPacket(TypeEncrypt.RSA));

    [Fact(DisplayName = "Test Send Client Packet Base64")]
    public void TestSendClientPacketBase64() => Assert.True(SendClientPacket(TypeEncrypt.Base64));

    [Fact(DisplayName = "Test Send Client Packet AES")]
    public void TestSendClientPacketAES() => Assert.True(SendClientPacket(TypeEncrypt.AES));

    [Fact(DisplayName = "Test Send Client Packet Only Compress")]
    public void TestSendClientPacketOnlyCompress() => Assert.True(SendClientPacket(TypeEncrypt.OnlyCompress, true));

    [Fact(DisplayName = "Test Send Client Packet Only Base64")]
    public void TestSendClientPacketOnlyBase64() => Assert.True(SendClientPacket(TypeEncrypt.OnlyBase64, true));

    static bool SendClientPacket(TypeEncrypt typeEncrypt, bool only = false){
        bool result = false;
        var message = "Hello World!";
        var client = new UDP.Client();
        client.OnStatus = (status)=>{
            if(status == ClientStatus.Connected)
                client.Send(Encoding.UTF8.GetBytes(message), 100, typeEncrypt);
        };

        var server = new UDP.Server();
        server.OnReceivedBytes += (bytes, groupID, ip) =>{
            if(only){
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

    [Fact(DisplayName = "Test Shutdown Server")]
    public void TestShutdownServer(){
        var client = new UDP.Client();
        var server = new UDP.Server();

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(2000);
        server.Stop();
        Thread.Sleep(client.ConnectTimeout + 1100);
        server.Start(IPAddress.Any, 25000);
        Thread.Sleep(5000);

        bool result = server.Status == ServerStatus.Running && client.Status == ClientStatus.Connected;

        server.Stop();
        client.Disconnect();
        Assert.True(result);
    }

    [Fact(DisplayName = "Test Disconnect Client")]
    public void TestDisconnectClient(){
        var client = new UDP.Client();
        var server = new UDP.Server();

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(2000);
        client.Disconnect();
        Thread.Sleep(server.ConnectedTimeout + 1100);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(5000);

        bool result = server.Status == ServerStatus.Running && client.Status == ClientStatus.Connected;

        server.Stop();
        client.Disconnect();
        Assert.True(result);
    }
}