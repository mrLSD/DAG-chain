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
    LastSeen: int option
}

type EventNetworkType =
    | GetMessage
    | SendMessage
    | GetNodes
    | Ping
    | Pong

type EventNetwork<'T> = {
    EventType: EventNetworkType
    Message: 'T 
}

type UdpConnect(addr: string, port: int) =
    let udpClient = new Net.Sockets.UdpClient()
    let recvUdpClient = new Net.Sockets.UdpClient(port)
    do
        udpClient.Connect(addr, port)
    let networkEvent = Event<EventNetwork<_>>()
    member this.NetworkEvent = networkEvent.Publish
    member this.Send(data: byte[]) = async {
        Logger.Debug("<- Send message: {msg}", BytesToString data)
        let ev = {
            EventNetwork.EventType = EventNetworkType.SendMessage
            Message = data
        } 
        networkEvent.Trigger(ev)
        return! udpClient.SendAsync(data, data.Length) |> Async.AwaitTask
    }
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
    member this.Subscribe(e: EventNetworkType, handler: EventNetwork<_> -> unit) =
        this.NetworkEvent
            |> Event.filter (fun (en: EventNetwork<_>) -> e.Equals en.EventType )
            |> Event.add handler
    member this.SendLoop = async {
        let msg = sprintf "Время: %O" DateTime.Now.TimeOfDay
        do! this.Send (Text.Encoding.UTF8.GetBytes msg) |> Async.Ignore
        do! Async.Sleep 1000
        do! this.SendLoop
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
    member this.GetAddresses (ev: EventNetwork<_>) =    
        let nodeAddresses = ev.Message
        ()
    member this.Run() =
        let randomNode = RandomNumberGenerator.GetInt32(bootstrapNodes.Length)
        let (node, port) = bootstrapNodes.[randomNode]
        // Connect to random Node
        
        client.Subscribe(EventNetworkType.GetNodes, this.GetAddresses)
