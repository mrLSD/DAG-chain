module DAG.Network

open System
open System.Security.Cryptography
open System.Text.Json
open DAG.Log
open DAG.Utils

/// Bootstrap Nodes array
let bootstrapNodes = [|
    ("127.0.0.1", 10001)
    ("127.0.0.1", 10002)
    ("127.0.0.1", 10003)
    ("127.0.0.1", 10004)
    ("127.0.0.1", 10005)
|]

/// Basic P2p Node structure
type P2PNode = {
    Address: string
    Port: string
    LastSeen: int option
}

/// Event Network common types
type EventNetworkType =
    | GetMessage
    | SendMessage
    | GetNodes 

/// Event structure of Network
type EventNetwork<'T> = {
    EventType: EventNetworkType
    Message: 'T 
}

/// Basic connection class
/// Contain Events messaging. All network behaviors are asynchronous.
/// Initialization via address and port.
/// When class released all connections will close.  
type UdpConnect(addr: string, port: int) =
    /// Client for Sending
    let udpClient = new Net.Sockets.UdpClient()
    /// Client for Receiving
    let recvUdpClient = new Net.Sockets.UdpClient(port)
    do
        udpClient.Connect(addr, port)
    /// Networks Event identifier
    let networkEvent = Event<EventNetwork<_>>()
    /// Network Event publisher
    member this.NetworkEvent = networkEvent.Publish
    /// Async Send data to remote client.
    /// Fire Event.
    member this.Send(data: byte[]) = async {
        Logger.Debug("<- Send message: {msg}", BytesToString data)
        let ev = {
            EventNetwork.EventType = EventNetworkType.SendMessage
            Message = data
        } 
        networkEvent.Trigger(ev)
        return! udpClient.SendAsync(data, data.Length) |> Async.AwaitTask
    }
    /// Async Get data from remote client
    /// Fire Event.
    member this.Get() = async {
        let! msg = recvUdpClient.ReceiveAsync() |> Async.AwaitTask
        Logger.Debug("-> Get message: {msg}", BytesToString msg.Buffer)
        let ev = {
            EventNetwork.EventType = EventNetworkType.GetMessage
            Message = msg.Buffer
        }
        networkEvent.Trigger(ev)
        return msg
    }
    /// Subscribe to specific EventNetworkType and handle Event with specific handler
    member this.Subscribe(e: EventNetworkType, handler: EventNetwork<_> -> unit) =
        this.NetworkEvent
            |> Event.filter (fun (en: EventNetwork<_>) -> e.Equals en.EventType )
            |> Event.add handler
    member this.SendLoop = async {
        let msg = sprintf "Время: %O" DateTime.Now.TimeOfDay
        do! Text.Encoding.UTF8.GetBytes msg |> this.Send |> Async.Ignore
        do! Async.Sleep 1000
        do! this.SendLoop
    }
    member this.GetLoop = async {
        do! this.Get() |> Async.Ignore
        do! this.GetLoop
    }
    /// Close all connections
    member this.Close() =
        udpClient.Close()
        recvUdpClient.Close()
    
    // Close connections
    interface IDisposable with
        member this.Dispose() = 
            this.Close()

/// Bootstrap Node structure
type NodeBootstrap() =
    member this.GetAddresses (ev: EventNetwork<_>) =
        let data: P2PNode =
            BytesToString ev.Message
            |> JsonSerializer.Deserialize
        let _ = data.Address
        ()
    /// Run boostraping from constant bootstrap nodes.
    /// Fetch random node and fire GetNodes event
    member this.Run() =
        let randomNode = RandomNumberGenerator.GetInt32 bootstrapNodes.Length
        let (node, port) = bootstrapNodes.[randomNode]
        // Connect to random Node
        let client = new UdpConnect(node, port)
        
        client.Subscribe(EventNetworkType.GetNodes, this.GetAddresses)
