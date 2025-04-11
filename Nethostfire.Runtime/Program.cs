using System.Net;
using Nethostfire;

internal class Program{
    static UDP.Server Server = new();
    static UDP.Client Client = new();
    private static void Main(string[] args){
        Server.Start(IPAddress.Any, 25000);
        Client.Connect(IPAddress.Loopback, 25000);
        Console.ReadLine();
    }
}