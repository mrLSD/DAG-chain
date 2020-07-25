/// DAG chain implementation

open System
open System.Text
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
    if tcpClient.Connected then
        Logger.Debug("listen")
        let stream = tcpClient.GetStream()

        Logger.Debug("listen Read")
        let rec data (): StringBuilder =
            let dataLen = (8 * 1024)
            let buffer = Array.zeroCreate dataLen
            let msg = StringBuilder()
            let len = stream.Read(buffer, 0, dataLen)
            printfn "[%A]" len
            if len > 0 then
                let msgData = msg.Append buffer
                printfn "%s" (msgData.ToString())
                printfn "%s" (msg.ToString())
                msgData.Append(data())
            else
                msg
        let msgData = data() 
        //let len = stream.Read(buffer, 0, dataLen)
        Logger.Debug("[{read}] {msg}", msgData.Length, msgData)
        if msgData.Length = 0 then
            stream.Close()
            tcpClient.Close()

        try
            let msg = StringToBytes (sprintf "Time: %O" DateTime.Now.TimeOfDay)
            stream.Write(msg, 0, msg.Length)
            Logger.Debug("listen sent {msg}", BytesToString msg)
        with
            | err -> 
                Logger.Debug("Connection closed")
                stream.Close()
                tcpClient.Close()
        listen tcpClient

let listenerFlow() =
    try
        let listener = listener()
        listener.Start()
        while true do
            listen(listener.AcceptTcpClient())
            printfn "New AcceptTcpClient"
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
    
    Threading.Thread.Sleep(30)
    Logger.Debug("sendClient try send")
    sendClient stream
    
let clientFlow() =
    try
        sendClient(Client().GetStream())
    with
        | err -> Logger.Error(err, "listenerFlow")

[<EntryPoint>]
let main argv =
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


    if argv.Length > 0 then
        clientFlow()
    else
        listenerFlow()
    0 // return an integer exit code
