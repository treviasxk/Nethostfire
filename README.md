# Nethostfire

![Preview](/imgs/banner.png)

Nethostfire is a library (netstandard2.1) to create UDP server and client in C#, with encryption support, Unity 3D integration and several other advanced features to facilitate communication between client and server.

![Preview](/imgs/preview.gif)

## Donate
 - Brazil
 PIX: trevias@live.com
 - International
 Paypal: trevias@live.com

## Main features
 - MySQL Client embedded.
 - Connected client detection system.
 - Manage all connected clients with server features.
 - Various types of shipping (single, group, all).
 - RSA, AES and Base64 encryption both on the server and on the client.
 - Automatic decryption.
 - Feature to send UDP bytes without losses.
 - Feature to send UDP bytes in enqueued.
 - Adapted to manipulate objects in Unity 3D.
 - Adapted for Cross-Server creation.
 - Adapted for high performance.
 - Adapted for Enter Play Mode on Unity.
 - Adapted for dedicated Unity build server.

## Requisites
 - Unity 2021.2 or above
 - .Net Netstandard 2.1 or above

## Unity installation
1 - Download the library **Nethostfire.dll** in [Releases](https://github.com/treviasxk/Nethostfire/releases)

2 - Move the file to the Assets folder of your Unity project **Assets/plugins/Nethostfire.dll**.

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
  - Server and Client - UDP (Coming soon)
  - Server and Client - TCP (Coming soon)