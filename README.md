# SharpNetworking
A TCP Libary

Usage =>

First you have to setup the events =>
```C#
 SharpNetworking.Initialize(OnMessageReceived, OnClientConnected, OnClientDisconnected, OnNetworkEvent);//Create the functions your self

void OnMessageReceived(SharpClient sender, int channel, byte[] rawData)
void OnClientConnected(SharpClient client)
void OnClientDisconnected(SharpClient client)
void OnNetworkEvent(SharpNetworkEvent e)
```

Server Host =>
```C#
SharpRoom room = new SharpRoom(RoomName,MaxConnections,Password);//There is more types of create room you can check them all
HostResult result;
bool succes = SharpNetworking.Host(PORT, room, out result);
```
And done.




Client Connect =>
```C#
SharpClient me = new SharpClient("Client's name");

ConnectResults result;
bool isSucces = SharpNetworking.Connect(IP, PORT, "", me, out result);
```

Ta da :P done.




Sending Message =>

```C#
//You can send specific clients, for that you have to create a List<SharpClient> and send as
SharpNetworking.SendMessage(Chanel,yourBytes,listyoucreated);
```

Or can send only one target,
```C#
SharpNetworking.SendMessage(Chanel,yourBytes,targetClient);
```

Or you can use SharpTargets enum
```C#
    /// Server Broadcast everyone(Including you) and Server Execute it too
    All,
	
	
    /// Server Broadcast everyone(Except you) And Server Execute it too
    Others,
	
	
    /// Only Server Receives and the Only Server Execute it
    Server,
	
	
    /// Server Broadcast everyone(Including you) But Server Doesn't Execute it
    ClientsOnly,
	
	
    /// Server Broadcast everyone(Except you) and Server doesn't execute it
    OtherClientsOnly
```

Thats all.



Every Client has knowladge about other clients.
Server sends other known clients to new client when it connects.

Have fun ^^




