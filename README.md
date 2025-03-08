# Nethostfire

![Preview](/screenshots/banner.png)

Nethostfire is a library (netstandard2.1) to create UDP server and client in C#, with encryption support, Unity 3D integration and several other advanced features to facilitate communication between client and server.

![Preview](/screenshots/preview.gif)

## Donate
 - Paypal: trevias@live.com
 - Github: [Sponsor](https://github.com/sponsors/treviasxk)

## Main features
 - MySQL Client embedded.
 - Connected client detection system.
 - Manage all connected clients with server features.
 - Various types of send (UNICAST, MULTICAST, BROADCAST).
 - RSA, AES and Base64 encryption both on the server and on the client.
 - Automatic decryption.
 - Suport JSON convert/deconvert.
 - Adapted for Cross-Server creation.
 - Adapted for high performance.

## .NET | VB.Net Project Installation
1 - Download the library **Nethostfire.dll** in [Releases](https://github.com/treviasxk/Nethostfire/releases)

2 - Move the file to the root folder of your .NET project.

3 - To add as a reference to your project, add the following xml tags to your project's .csproj file.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Reference Include="Nethostfire.dll"></Reference>
  </ItemGroup>
 ...
</Project>
```
4 - Then import the namespace `'using Nethostfire;'` into your scripts and then restore the project with `'dotnet restore'`.