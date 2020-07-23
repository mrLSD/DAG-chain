/// DAG chain implementation

open System
open DAG
open DAG.Bootstrap
open DAG.State
open DAG.Storage
open System.Net
open DAG.Log
open Utils

let addr = "127.0.0.1"
let port = 3000

let client() = 
    let connect = new Sockets.TcpClient()
    connect.Connect(addr, port)
    connect

let listener() = Sockets.TcpListener(IPAddress.Parse(addr), port)

let rec listen(tcpClient: Sockets.TcpClient): unit =
    Logger.Debug("listen")
    let stream = tcpClient.GetStream()

    if stream.DataAvailable then
        Logger.Debug("listen Read")
        let buffer = Array.zeroCreate 256
        let read = stream.Read(buffer, 0, 256)
        Logger.Debug("[{read}] {msg}", read, BytesToString buffer)
    
        let msg = StringToBytes (sprintf "Time: %O" DateTime.Now.TimeOfDay)
        stream.Write(msg, 0, msg.Length)
        Logger.Debug("listen sent {msg}", msg)
        listen tcpClient

let listenerFlow() =
    try
        let listener = listener()
        listener.Start()
        listen(listener.AcceptTcpClient())
    with
        | err -> Logger.Error(err, "listenerFlow")

let Client() =
        let connect = new Sockets.TcpClient()
        connect.Connect(addr, port)
        connect
    
let rec sendClient(stream: Sockets.NetworkStream): unit =
    Logger.Debug("sendClient")
    let msg = StringToBytes (sprintf "Time: %O" DateTime.Now.TimeOfDay)
    Logger.Debug("sendClient Write {msg}", BytesToString msg)
    stream.Write(msg, 0, msg.Length)
    
    Logger.Debug("sendClient Read")
    let buffer = Array.zeroCreate 256
    let read = stream.Read(buffer, 0, 256)
    Logger.Debug("[{read}] {msg}", read, BytesToString buffer)
    
    Threading.Thread.Sleep(3000)
    Logger.Debug("sendClient try send")
    sendClient stream
    
let clientFlow() =
    try
        sendClient(Client().GetStream())
    with
        | err -> Logger.Error(err, "listenerFlow")

[<EntryPoint>]
let main argv =
    //listenerFlow()
    clientFlow()
//    let listener  = Network.TcpConnectC("127.0.0.1", 3000)
//    let listener  = Network.TcpConnectListener("127.0.0.1", 3000)
//    let client  = Network.TcpConnectClient("127.0.0.1", 3000)
//    [ listener.Listen;  ]
//    |> Async.Parallel
//    |> Async.Ignore
//    |> Async.RunSynchronously
    
//    let listener  = Network.UdpListener("127.0.0.1", 3001)
//    let appState = {
//        AppState.Storage = Storage("data")
//        Listener = listener
//    }
//    let bs = NodeBootstrap(appState)
//    bs.Discovery
    //[listener.GetLoop]
    //[bs.Run; listener.GetLoop]
    //[client.GetLoop; client.SendLoop; client.GetEnv; bs.Run]
//    [bs.Run]
//    |> Async.Parallel
//    |> Async.Ignore
//    |> Async.RunSynchronously

    0 // return an integer exit code
