using System.Net;
using static Nethostfire.Tests.Utils;

namespace Nethostfire.Tests;

public class Nethostfire{
    [Fact(DisplayName = "Test Client Send Packet")]
    public void TestSendClientPacket() => Assert.True(SendClientPacket(TypeEncrypt.None));

    [Fact(DisplayName = "Test Client Send Packet Compress")]
    public void TestSendClientPacketCompress() => Assert.True(SendClientPacket(TypeEncrypt.Compress));

    [Fact(DisplayName = "Test Client Send Packet RSA")]
    public void TestSendClientPacketRSA() => Assert.True(SendClientPacket(TypeEncrypt.RSA));

    [Fact(DisplayName = "Test Client Send Packet Base64")]
    public void TestSendClientPacketBase64() => Assert.True(SendClientPacket(TypeEncrypt.Base64));

    [Fact(DisplayName = "Test Client Send Packet AES")]
    public void TestSendClientPacketAES() => Assert.True(SendClientPacket(TypeEncrypt.AES));

    [Fact(DisplayName = "Test Client Send Packet Only Compress")]
    public void TestSendClientPacketOnlyCompress() => Assert.True(SendClientPacket(TypeEncrypt.OnlyCompress));

    [Fact(DisplayName = "Test Client Send Packet Only Base64")]
    public void TestSendClientPacketOnlyBase64() => Assert.True(SendClientPacket(TypeEncrypt.OnlyBase64));

    [Fact(DisplayName = "Test Client Send Packet Null")]
    public void TestSendClientPacketNull() => Assert.True(SendClientPacket());
    
    [Fact(DisplayName = "Test Client Send Packet Object")]
    public void TestSendClientPacketObject() => Assert.True(SendClientPacket(false));

   
    [Fact(DisplayName = "Test Server Send Packet")]
    public void TestSendServerPacket() => Assert.True(SendServerPacket(TypeEncrypt.None));

    [Fact(DisplayName = "Test Server Send Packet Compress")]
    public void TestSendServerPacketCompress() => Assert.True(SendServerPacket(TypeEncrypt.Compress));

    [Fact(DisplayName = "Test Server Send Packet RSA")]
    public void TestSendServerPacketRSA() => Assert.True(SendServerPacket(TypeEncrypt.RSA));

    [Fact(DisplayName = "Test Server Send Packet Base64")]
    public void TestSendServerPacketBase64() => Assert.True(SendServerPacket(TypeEncrypt.Base64));

    [Fact(DisplayName = "Test Server Send Packet AES")]
    public void TestSendServerPacketAES() => Assert.True(SendServerPacket(TypeEncrypt.AES));

    [Fact(DisplayName = "Test Server Send Packet OnlyCompress")]
    public void TestSendServerPacketOnlyCompress() => Assert.True(SendServerPacket(TypeEncrypt.OnlyCompress));

    [Fact(DisplayName = "Test Server Send Packet OnlyBase64")]
    public void TestSendServerPacketOnlyBase64() => Assert.True(SendServerPacket(TypeEncrypt.OnlyBase64));

    [Fact(DisplayName = "Test Server Send Packet Null")]
    public void TestSendServerPacketNull() => Assert.True(SendServerPacket());
    
    [Fact(DisplayName = "Test Server Send Packet Object")]
    public void TestSendServerPacketObject() => Assert.True(SendServerPacket(false));

    [Fact(DisplayName = "Test Server Send Packet Group")]
    public void TestSendServerPacketGroup() => Assert.True(SendServerPacketGroup());

    [Fact(DisplayName = "Test Server Send Packet Group Null")]
    public void TestSendServerPacketGroupNull() => Assert.True(SendServerPacketGroup(false));

    [Fact(DisplayName = "Test Server Shutdown")]
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

    [Fact(DisplayName = "Test Client Disconnect")]
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
        server.Dispose();
        client.Dispose();
        Assert.True(result);
    }

    [Fact(DisplayName = "Test Client Limit PPS")]
    public void TestLimitPPSClientx() => Assert.True(TestLimitPPSClient());
    
    [Fact(DisplayName = "Test Client Limit Group PPS")]
    public void TestLimitGroupPPSClientx() => Assert.True(TestLimitPPSClient(true));
}