# Nethostfire

![Preview](/Documentation/images/banner.png)

Nethostfire is a library (netstandard2.1) designed to create UDP servers and clients in C#, with support for encryption, Unity 3D integration, and advanced features to facilitate communication between clients and servers.

![Preview](/Documentation/images/preview.gif)

## Features

- **Encryption Support**: Includes RSA, AES, and Base64 encryption for secure communication.
- **Unity 3D Integration**: Adapted for Unity 3D projects, including support for Enter Play Mode and dedicated Unity server builds.
- **High Performance**: Optimized for high-performance networking.
- **Cross-Server Communication**: Supports creating cross-server communication systems.
- **MySQL Integration**: Embedded MySQL client for database operations.
- **JSON Serialization**: Built-in JSON serialization and deserialization.
- **Packet Loss Prevention**: Features to send UDP packets without losses or in an enqueued manner.
- **Client and Server Management**: Tools to manage connected clients and server sessions.

## Getting Started

### Installation for Unity 3D Projects
1. Download the **Nethostfire.dll** from the [Releases](https://github.com/treviasxk/Nethostfire/releases) page.
2. Place the file in your Unity project's `Assets/plugins/` folder.
3. Import the namespace in your scripts:
  ```csharp
  using Nethostfire;
  ```

### Installation for .NET Projects
1. Download the Nethostfire.dll from the Releases page.
2. Place the file in the root folder of your .NET project.
3. Add the following to your **.csproj** file:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Reference Include="Nethostfire.dll"></Reference>
  </ItemGroup>
  ...
</Project>
```

### Installation for NuGet
Official Nethostfire NuGet page, you can find it [here](https://www.nuget.org/packages/Nethostfire/) or use the command line below to add package in project.
```
  dotnet add package Nethostfire --version 0.9.4
```

---

### Usage
#### Creating a Server
  ```csharp
using System.Net;
using Nethostfire.UDP;

var server = new Server();
server.Start(IPAddress.Any, 25000);
server.DataReceived += (sender, args) => {
    Console.WriteLine($"Received data: {Encoding.UTF8.GetString(args.Data)}");
};
  ```

#### Creating a Client
  ```csharp
using System.Net;
using Nethostfire.UDP;

var client = new Client();
client.Connect(IPAddress.Loopback, 25000);
client.DataReceived += (sender, args) => {
    Console.WriteLine($"Received data: {Encoding.UTF8.GetString(args.Data)}");
};
client.Send("Hello, Server!", 0);
  ```
---

### Advanced Features
#### TypeEncrypt
The `TypeEncrypt` enum allows you to define how data is encrypted when being sent over the network. This ensures secure communication between clients and servers. Below are the available encryption types:

- **None**: The data is sent without any encryption.
- **AES**: The data is encrypted using AES (Advanced Encryption Standard) and automatically decrypted upon reaching its destination.
- **RSA**: The data is encrypted using RSA (Rivest–Shamir–Adleman) and automatically decrypted upon reaching its destination.
- **Base64**: The data is encoded in Base64 format and automatically decoded upon reaching its destination.
- **Compress**: The data is compressed before being sent and automatically decompressed upon reaching its destination.
- **OnlyBase64**: The data is encoded in Base64 format, but if the destination is a server, it will not be decoded.
- **OnlyCompress**: The data is compressed, but if the destination is a server, it will not be decompressed.

##### Example: Sending Packets with TypeEncrypt
```csharp
// Sending a packet without encryption
client.Send("Plain Message", 1, TypeEncrypt.None);

// Sending a packet encrypted with AES
client.Send("Secure Message", 1, TypeEncrypt.AES);

// Sending a packet compressed
client.Send("Compressed Message", 1, TypeEncrypt.Compress);
  ```

#### TypeShipping
The `TypeShipping` enum allows you to define how UDP packets are sent and handled, providing options for reliability and performance. Below are the available modes:

- **None**: No special handling is applied to the packet.
- **WithoutPacketLoss**: Ensures packets are delivered without loss but does not queue them for order.
- **WithoutPacketLossEnqueue**: Ensures packets are delivered without loss and queues them to maintain order. Not recommended for high-demand scenarios due to potential delays.

##### Example: Sending Packets with TypeShipping
```csharp
// Sending a packet without packet loss
client.Send("Reliable Message", 1, TypeEncrypt.None, TypeShipping.WithoutPacketLoss);

// Sending a packet without packet loss and with queuing
client.Send("Ordered Reliable Message", 1, TypeEncrypt.AES, TypeShipping.WithoutPacketLossEnqueue);
  ```
  
#### MySQL Integration
Nethostfire includes built-in support for MySQL, allowing you to connect to a database and perform operations directly from your application. This feature is useful for storing and retrieving data in real-time applications.

##### Example: Connecting to a MySQL Database
```csharp
using System.Net;
using Nethostfire.MySQL;

var mysql = new MySQL();
mysql.Connect(IPAddress.Parse("127.0.0.1"), 3306, "username", "password", "database");
```
---

## Contribute
Contributions are welcome! You can fork the repository and submit a pull request or sponsor the project to support its development.
 - Github: [Sponsor](https://github.com/sponsors/treviasxk)
 - Paypal: trevias@live.com