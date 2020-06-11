/// DAG chain implementation

open System
open DAG
open DAG.Network
open DAG.State
open DAG.Storage

[<EntryPoint>]
let main argv =
    let client  = Network.UdpClient("127.0.0.1", 300)
//    let listener  = Network.UdpListener(3000)
//    let appState = {
//        AppState.Storage = Storage("db")
//        Listener = listener
//    }
//    let bs = NodeBootstrap(appState)
    //[listener.GetLoop]
    [client.SendLoop]
    //[client.GetLoop; client.SendLoop; client.GetEnv; bs.Run]
    //[client.GetLoop; bs.Run]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
