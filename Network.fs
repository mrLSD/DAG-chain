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
open DAG.Utils

let bootstrapNodes = [|
    ("127.0.0.1", 10001)
    ("127.0.0.1", 10002)
    ("127.0.0.1", 10003)
    ("127.0.0.1", 10004)
    ("127.0.0.1", 10005)
|]

type P2PNode = {
    Address: string
    Port: string
    LastSeen: DateTime option
}

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

/// UDP connection for Node
/// Contain Events
type UdpConnect(addr: string, port: int) =
    let udpClient = new Net.Sockets.UdpClient()
    let recvUdpClient = new Net.Sockets.UdpClient(port)
    do
        udpClient.Connect(addr, port)
    let networkEvent = Event<EventNetwork>()
    member this.NetworkEvent = networkEvent.Publish
    member this.Send(ev: EventNetwork) = async {
        let msg = ev.Encode()
        Logger.Debug("<- Send message: {msg}", ev)
        let! res = udpClient.SendAsync(msg, msg.Length) |> Async.AwaitTask
        return res
    }
    member this.Get() = async {
        let! msg = recvUdpClient.ReceiveAsync() |> Async.AwaitTask
        Logger.Debug("-> Get message: {msg}", BytesToString msg.Buffer)
        let ev = EventNetwork.Decode(msg.Buffer)
        networkEvent.Trigger(ev)
        return msg
    }
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
    member this.Close() =
        udpClient.Close()
        recvUdpClient.Close()
    
    // Close connections
    interface IDisposable with
        member this.Dispose() = 
            this.Close()

type NodeBootstrap() =
    member this.HandlerGetNodes ev =    
        let nodeAddresses = Json.deserialize<MessageGetNodes> ev
        ()
    member this.Run() =
        let randomNode = RandomNumberGenerator.GetInt32(bootstrapNodes.Length)
        let (node, port) = bootstrapNodes.[randomNode]
        // Connect to random Node
        let client = new UdpConnect(node, port)
        client.Subscribe(EventNetworkType.GetNodes, this.HandlerGetNodes)
