using System.Net;
using static Nethostfire.Tests.Utils;

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
    public void TestSendClientPacketOnlyCompress() => Assert.True(SendClientPacket(TypeEncrypt.OnlyCompress));

    [Fact(DisplayName = "Test Send Client Packet Only Base64")]
    public void TestSendClientPacketOnlyBase64() => Assert.True(SendClientPacket(TypeEncrypt.OnlyBase64));
   
    [Fact(DisplayName = "Test Send Server Packet")]
    public void TestSendServerPacket() => Assert.True(SendServerPacket(TypeEncrypt.None));

    [Fact(DisplayName = "Test Send Server Packet Compress")]
    public void TestSendServerPacketCompress() => Assert.True(SendServerPacket(TypeEncrypt.Compress));

    [Fact(DisplayName = "Test Send Server Packet RSA")]
    public void TestSendServerPacketRSA() => Assert.True(SendServerPacket(TypeEncrypt.RSA));

    [Fact(DisplayName = "Test Send Server Packet Base64")]
    public void TestSendServerPacketBase64() => Assert.True(SendServerPacket(TypeEncrypt.Base64));

    [Fact(DisplayName = "Test Send Server Packet AES")]
    public void TestSendServerPacketAES() => Assert.True(SendServerPacket(TypeEncrypt.AES));

    [Fact(DisplayName = "Test Send Server Packet OnlyCompress")]
    public void TestSendServerPacketOnlyCompress() => Assert.True(SendServerPacket(TypeEncrypt.OnlyCompress));

    [Fact(DisplayName = "Test Send Server Packet OnlyBase64")]
    public void TestSendServerPacketOnlyBase64() => Assert.True(SendServerPacket(TypeEncrypt.OnlyBase64));

    [Fact(DisplayName = "Test Shutdown Server")]
    public void TestShutdownServer(){
        var result = false;
        var client = new UDP.Client();
        var server = new UDP.Server();

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(3000);
        server.Stop();
        Thread.Sleep(client.ConnectTimeout + 1100);
        server.Start(IPAddress.Any, 25000);

        server.OnConnected = (ip) =>{
            result = client.Status == SessionStatus.Connected && server.Sessions.GetStatus(ip) == SessionStatus.Connected;
        };

        Thread.Sleep(5000);
        server.Stop();
        client.Disconnect();
        Assert.True(result);
    }

    [Fact(DisplayName = "Test Disconnect Client")]
    public void TestDisconnectClient(){
        var result = false;
        var client = new UDP.Client();
        var server = new UDP.Server();

        server.Start(IPAddress.Any, 25000);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);
        Thread.Sleep(3000);
        client.Disconnect();
        Thread.Sleep(server.ConnectedTimeout + 1100);
        client.Connect(IPAddress.Parse("127.0.0.1"), 25000);

        server.OnConnected = (ip) =>{
            result = client.Status == SessionStatus.Connected && server.Sessions.GetStatus(ip) == SessionStatus.Connected;
        };

        Thread.Sleep(5000);
        server.Stop();
        client.Disconnect();
        Assert.True(result);
    }
}