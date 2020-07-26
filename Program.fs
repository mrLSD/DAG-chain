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

let rec listen(tcpClient: Sockets.TcpClient) = async {
    if tcpClient.Connected then
        Logger.Debug("listen")
        let stream = tcpClient.GetStream()

        Logger.Debug("listen Read")
        let rec data ()  = async {
            let dataLen = (16 * 1024)
            let buffer = Array.zeroCreate dataLen
            let msg = StringBuilder()
            let! len = stream.ReadAsync(buffer, 0, dataLen) |> Async.AwaitTask
            if stream.DataAvailable && len > 0 then
                return msg.Append(BytesToStringLength(buffer, len)).Append(data())
            else
                return msg.Append(BytesToStringLength(buffer, len))
        }
        let! msgData = data() 
        Logger.Debug("[{read}] {msg}", msgData.Length, msgData)
        if msgData.Length = 0 then
            stream.Close()
            tcpClient.Close()

        try
            let msg = StringToBytes (sprintf "Time: %O" DateTime.Now.TimeOfDay)
            do! stream.WriteAsync(msg, 0, msg.Length) |> Async.AwaitTask
            Logger.Debug("listen sent {msg}", BytesToString msg)
        with
            | err -> 
                Logger.Debug("Connection closed")
                stream.Close()
                tcpClient.Close()
        return! listen(tcpClient) |> Async.Ignore
}

let listenerFlow() = async {
    try
        let listener = listener()
        listener.Start()
        while true do
            do! listen(listener.AcceptTcpClient())
            Logger.Information("New AcceptTcpClient")
    with
        | err -> Logger.Error(err, "listenerFlow")
}    

let Client() =
    let connect = new Sockets.TcpClient()
    connect.Connect(addr, port)
    connect
    
let rec sendClient(stream: Sockets.NetworkStream) = async {
    Logger.Debug("sendClient")
    let mutable sent = false
    let! _= Async.StartChild(async{
        Logger.Debug("sendClient async")
        let msg = StringToBytes (sprintf "Time: %O" DateTime.Now.TimeOfDay)
        Logger.Debug("sendClient Write {msg}", BytesToString msg)
        do! stream.WriteAsync(msg, 0, msg.Length) |> Async.AwaitTask
        
        Logger.Debug("sendClient Read")
        let dataLen = (16 * 1024)
        let buffer = Array.zeroCreate dataLen
        let! len = stream.ReadAsync(buffer, 0, dataLen) |> Async.AwaitTask
        Logger.Debug("[{len}] {msg}", len, BytesToString buffer)
        sent <- true
    })
    
    Logger.Information("Wait")
    
    do! Async.Sleep 10000
    if sent then
        Logger.Debug("sendClient try send\n")
        return! sendClient(stream) |> Async.Ignore
    else
        Logger.Error("Didn't sent\n")
}
    
let clientFlow() = async {
    try
        do! sendClient(Client().GetStream()) |> Async.Ignore
    with
        | err -> Logger.Error(err, "listenerFlow")
}

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
        [clientFlow()]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously
    else
        [listenerFlow()]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously
    0 // return an integer exit code
