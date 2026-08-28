# ChatApp

A chat application built with WPF (.NET 8) and raw TCP sockets. It's split into two desktop apps: a **server** that accepts client 
connections and broadcasts messages, and a **client** that connects to a server, sends messages, and displays incoming chat history. 
Since it's plain TCP, clients can connect over a local network or over the internet, as long as the server's IP/port is reachable 
(e.g. port forwarding if the server is behind NAT).

## What it does

- **ChatAppServer** — binds to a chosen IP/port, listens for incoming client connections, tracks connected usernames, and broadcasts every message it receives to all connected clients. It also shows a live message history and connected-user list, and lets the server operator send its own messages to everyone.
- **ChatApp** (client) — connects to a server by IP/port, registers a username, and lets the user send and receive chat messages in real time.

Messages are exchanged over TCP using a simple length-prefixed JSON protocol: each message is a 4-byte little-endian length prefix followed by a JSON-encoded `Payload` (`Sender` + `Message`).

## Requirements

- Windows (WPF app)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.14+) if you want to build/debug via the `.sln`

## Install / Build

Clone the repo, then build with the .NET SDK:

```bash
git clone <repo-url>
cd ChatApp
dotnet build ChatApp.sln
```

Or open `ChatApp.sln` in Visual Studio and build the solution there.

## Usage

1. **Start the server**
   - Run the `ChatAppServer` project (`dotnet run --project ChatAppServer` or F5 in Visual Studio).
   - Set the IP and port to bind to, then click **Start Server**.
2. **Connect clients**
   - Run the `ChatApp` project (`dotnet run --project ChatApp`, one instance per client).
   - Enter the server's IP, port, and a username, then connect.
3. **Chat**
   - Connected clients can send messages, which the server broadcasts to everyone connected.
   - The server window shows connect/disconnect events and full message history.

To run multiple clients locally for testing, launch several instances of `ChatApp` and connect them all to the same server address.
