/// # DAG Network Layer
///
/// ## Bootstrap & discovery Nodes
/// * GetNode message - request Nodes from random Bootstrap Node - no more
/// than 2500 Bootstrap Node updates Requested Node last seen time
/// * Bootstrap return nodes last seen within 3 hours
/// * If nodes count less than 1000 ask Nodes form another random but not
/// same Bootstrap Node
/// * Every 20 min Node send Ping message to random Bootstrap Node and
/// return Pong message
/// * When Bootstrap Node receive PING request it's update Requested Node
/// last seen time
///
/// ## Event Messages
/// * When some of Events fired (Current Node create own Event or received
/// form other Nodes) Current Node take random 50 Nodes from 3 hours last
/// seen Nodes. If nodes count less than 25, take summary with previously
/// taken nodes - random Nodes from 24 hours last seen Nodes.
/// * Send message for selected Nodes and store it to Storage
/// * If received message (Event) already exist in the Storage - ignore
/// that message 
module DAG.Network

open System
open System.Security.Cryptography
open FSharp.Json
open DAG.Log
open DAG.State
open DAG.Utils

/// Bootstrap Nodes array
let bootstrapNodes = [|
    ("127.0.0.1", 3000)
//    ("127.0.0.1", 10001)
//    ("127.0.0.1", 10002)
//    ("127.0.0.1", 10003)
//    ("127.0.0.1", 10004)
//    ("127.0.0.1", 10005)
|]

/// Basic P2p Node structure
type P2PNode = {
    Address: string
    Port: string
    LastSeen: DateTime option
}

/// Event Network common types
type EventNetworkType =
    | GetMessage
    | SendMessage
    | GetNodes
    | Ping
    | Pong

/// EventNetwork message structure
type EventNetwork = {
    EventType: EventNetworkType
    Message: string
} with
    member this.Encode() =
        let data = Json.serialize this
        StringToBytes data
    static member Decode(data: byte[]): EventNetwork =
        Json.deserialize (BytesToString data)

/// GetNodes Message 
type MessageGetNodes = {
    Nodes: P2PNode[]
}
  
type MessageSend = {
    data: string
} with
    member this.CreateEvent() =
        {
            EventNetwork.EventType = EventNetworkType.SendMessage
            Message = Json.serialize this
        }
        
type MessagePing = {
    senderAddr: string
    senderPort: int
} with
    member this.CreateEvent() =
        {
            EventNetwork.EventType = EventNetworkType.Ping
            Message = Json.serialize this
        }
    static member GetData(msg): MessagePing = 
        Json.deserialize msg    

type MessagePong =
    | Success
    member this.CreateEvent() =
        {
            EventNetwork.EventType = EventNetworkType.Ping
            Message = ""
        }

/// Basic connection class
/// Contain Events messaging. All network behaviors are asynchronous.
/// Initialization via address and port.
/// When class released all connections will close.
type UdpConnect(addr: string, port: int, sender: bool) =
    /// Init multi client
    let udpConncet =
        if sender then
            /// Client for Sending
            let connect = new Net.Sockets.UdpClient()
            connect.Connect(addr, port)
            connect
        else
            /// Client for Receiving
            new Net.Sockets.UdpClient(port)

    /// Networks Event identifier
    let networkEvent = Event<EventNetwork>()
    member this.Addr = addr
    member this.Port = port 
    /// Network Event publisher
    member this.NetworkEvent = networkEvent.Publish
    /// Async Send data to remote client.
    /// Fire Event.
    member this.Send(ev: EventNetwork) = async {
        let msg = ev.Encode()
        Logger.Debug("<- Send message: {msg}", ev)
        return! udpConncet.SendAsync(msg, msg.Length) |> Async.AwaitTask        
    }
    /// Async Get data from remote client
    /// Fire Event.
    member this.Get() = async {
        let! msg = udpConncet.ReceiveAsync() |> Async.AwaitTask
        Logger.Debug("-> Get message: {msg}", BytesToString msg.Buffer)
        let ev = EventNetwork.Decode(msg.Buffer)
        networkEvent.Trigger(ev)
        return msg
    }
    /// Subscribe to specific EventNetworkType and handle Event with specific handler
    member this.Subscribe(e: EventNetworkType, handler: string -> unit) =
        this.NetworkEvent
            |> Event.filter (fun (en: EventNetwork) -> e.Equals en.EventType )
            |> Event.add (fun ev -> handler ev.Message)
    member this.SendLoop = async {
        let msg = sprintf "Время: %O" DateTime.Now.TimeOfDay
        let ev = {
            MessageSend.data = msg
        }        
        do! this.Send(ev.CreateEvent()) |> Async.Ignore
        do! Async.Sleep 1000
        do! this.SendLoop
    }
    member this.GetEnv = async {
        this.Subscribe(EventNetworkType.SendMessage, fun ev ->
                let nodeAddresses = Json.deserialize<MessageSend> ev
                printfn "GetEnv: %A" nodeAddresses
            )
    }
    member this.GetLoop = async {
        do! this.Get() |> Async.Ignore
        do! this.GetLoop
    }
    /// Close all connections
    member this.Close() =
        udpConncet.Close()
    
    // Close connections
    interface IDisposable with
        member this.Dispose() = 
            this.Close()

let UdpClient(addr: string, port: int) = new UdpConnect(addr, port, true)
let UdpListener(addr: string, port: int) = new UdpConnect(addr, port, false)

/// Bootstrap Node structure
type NodeBootstrap(state: AppState<UdpConnect>) =
    member this.HandlerGetNodes ev =
        //let _ = state.Storage.Get("nodes")
        //let nodeAddresses = Json.deserialize<MessageGetNodes> ev
        printfn "\t# HandlerGetNodes: %s" ev
        ()
    /// Run bootstrapping from constant bootstrap nodes.
    /// Fetch random node and fire GetNodes event
    member this.Run = async {
        state.Listener.Subscribe(EventNetworkType.GetNodes, this.HandlerGetNodes)
        state.Listener.Subscribe(EventNetworkType.Pong, fun _ ->
            printfn "\t# EventNetworkType.Pong"
            ()
        )

        let randomNode = RandomNumberGenerator.GetInt32 bootstrapNodes.Length
        let (node, port) = bootstrapNodes.[randomNode]        
        let ev = {            
            MessagePing.senderAddr = state.Listener.Addr
            senderPort = state.Listener.Port
        }
        // Connect to random Node
        do! UdpClient(node, port)
                .Send(ev.CreateEvent())
                |> Async.Ignore
    }
    member this.Discovery = 
        state.Listener.Subscribe(EventNetworkType.Ping, fun ev -> 
            printfn "\t# Subscribe.Ping: %s" ev
            let msg = MessagePing.GetData ev 
            let ev = MessagePong.Success
            // Send synchronously
            UdpClient(msg.senderAddr, msg.senderPort)
                .Send(ev.CreateEvent())
                |> Async.Ignore
                |> Async.RunSynchronously
        )
