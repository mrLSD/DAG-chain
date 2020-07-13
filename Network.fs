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
open System.Net
open System.Net.Sockets
open System.Text
open FSharp.Json
open DAG.Log
open DAG.Utils

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

type TcpConnectClient (addr: string, port: int) =
    /// Networks Event identifier
    let networkEvent = Event<EventNetwork>()
    let connect =
            let connect = new Net.Sockets.TcpClient()
            connect.Connect(addr, port)
            connect
//    static member Listener (addr: string, port: int) =
//            let connect = Net.Sockets.TcpListener(IPAddress.Parse(addr), port)
//            connect.Start()
//            connect
    /// Network Event publisher
    member this.NetworkEvent = networkEvent.Publish
    member this.Get() =
        let stream = connect.GetStream()
        let buffer: byte[] = Array.zeroCreate 1024
        let mutable msg = StringBuilder()
        let rec data () =
            if stream.DataAvailable then
                let _ = stream.Read(buffer, 0, 1024)
                msg <- msg.Append buffer
                do data()
        data()
        Logger.Debug("-> Get message: {msg}", msg.ToString())
        msg.ToString()
    member this.Send(ev: EventNetwork) =
        let stream = connect.GetStream()
        let msg = ev.Encode()
        Logger.Debug("<- Send message: {msg}", ev)
        stream.Write(msg, 0, msg.Length)

type TcpConnectListener (addr: string, port: int) =
    /// Networks Event identifier
    let networkEvent = Event<EventNetwork>()
    let connect =
            let connect = Net.Sockets.TcpListener(IPAddress.Parse(addr), port)
            connect.Start()
            connect
    /// Network Event publisher
    member this.NetworkEvent = networkEvent.Publish
    member this.Listen() = async {
        let rec listen () =
            let stream = connect.AcceptTcpClient().GetStream()
            let res = this.Get(stream)
            this.Send(stream, StringToBytes "Listener")
            Logger.Debug("Listen: ", res)
            listen()
        listen()
    }
    member this.Get(stream: NetworkStream): string =
        let buffer: byte[] = Array.zeroCreate 1024
        let mutable msg = StringBuilder()
        let rec data () =
            if stream.DataAvailable then
                let _ = stream.Read(buffer, 0, 1024)
                msg <- msg.Append buffer
                do data()
        data()
        Logger.Debug("-> Get message: {msg}", msg.ToString())
        msg.ToString()
    member this.Send(stream: NetworkStream, msg: byte[]) =
        Logger.Debug("<- Send message: {msg}", msg)
        stream.Write(msg, 0, msg.Length)

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
//    member this.SendLoop = async {
//        let msg = sprintf "Время: %O" DateTime.Now.TimeOfDay
//        let ev = {
//            MessageSend.data = msg
//        }        
//        do! this.Send(ev.CreateEvent()) |> Async.Ignore
//        do! Async.Sleep 1000
//        do! this.SendLoop
//    }
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
