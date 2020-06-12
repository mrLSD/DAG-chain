/// DAG chain implementation

open System
open DAG
open DAG.Network
open DAG.State
open DAG.Storage

[<EntryPoint>]
let main argv =
//    let client  = Network.UdpClient("127.0.0.1", 3000)
    let listener  = Network.UdpListener("127.0.0.1", 3001)
    let appState = {
        AppState.Storage = Storage("data")
        Listener = listener
    }
    let bs = NodeBootstrap(appState)
    //[listener.GetLoop]
    [bs.Run; bs.Discovery; listener.GetLoop]
    //[client.GetLoop; client.SendLoop; client.GetEnv; bs.Run]
    //[client.GetLoop; bs.Run]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
