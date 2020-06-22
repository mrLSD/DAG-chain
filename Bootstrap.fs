module DAG.Bootstrap

open System
open DAG.State
open DAG.Network
open FSharp.Json
open System.Security.Cryptography

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
