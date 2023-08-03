# Nethostfire

![Preview](/Images/Banner.png)

Nethostfire is a library (netstandard2.1) to create UDP server and client in C#, with encryption support, Unity 3D integration and several other advanced features to facilitate communication between client and server.

![Preview](/Images/Sample.gif)

## Donate
 - Brazil
 PIX: trevias@live.com
 - International
 Paypal: trevias@live.com

## Main features
 - PPS bandwidth control for each connected client.
 - PPS bandwidth control for each groupID of shipments.
 - Bytes bandwidth control for each connected client.
 - Bytes bandwidth control for each groupID of shipments.
 - Connected client detection system.
 - Manage all connected clients with server resources.
 - Various types of submissions (single, group, all).
 - RSA, AES and Base64 encryption both on the server and on the client.
 - Automatic decryption.
 - Resource to send UDP bytes without losses.
 - Resource to send UDP bytes in enqueued.
 - Adapted to manipulate objects in Unity 3D.
 - Adapted for Cross-Server creation.
 - Adapted for high performance.
 - Adapted for Enter Play Mode on Unity.
 - Adapted for dedicated Unity build server.
 - Connection statistics interface in Unity for the client.

## Requisites
 - Unity 2021.2 or above
 - .Net Netstandard 2.1 or above

## Unity installation
1 - Download the library **Nethostfire.dll** in [Releases](https://github.com/treviasxk/Nethostfire/releases)

2 - Move the file to the Assets folder of your Unity project **Assets/bin/debug/Nethostfire.dll**.

3 - Then import the namespace `'using Nethostfire;'` in your scripts.

## .NET | VB.Net Project Installation
1 - Download the library **Nethostfire.dll** in [Releases](https://github.com/treviasxk/Nethostfire/releases)

2 - Move the file to the root folder of your .NET project.

3 - To add as a reference to your project, add the following xml tags to your project's .csproj file.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Reference Include="Nethostfire.dll">
      <SpecificVersion>False</SpecificVersion> 
    </Reference>
  </ItemGroup>
 ...
</Project>
```
4 - Then import the namespace `'using Nethostfire;'` into your scripts and then restore the project with `'dotnet restore'`.

## Documentation
  - [Server and Client - UDP](UDP/README.md)
  - Server and Client - TCP (Coming soon)

## Projects Examples
  - (Coming soon)

## Youtube Tutorials  
  - <img src="https://cdn.jsdelivr.net/gh/hampusborgos/country-flags@main/svg/br.svg" width="15"> [Introdução projeto Open-Source para criação de servidores dedicado - Nethostfire](https://youtu.be/T9Mt-7KJBTI) | Trevias Xk