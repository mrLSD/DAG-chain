/// DAG chain implementation

open DAG
open DAG.Bootstrap
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
    bs.Discovery
    //[listener.GetLoop]
    [bs.Run; listener.GetLoop]
    //[client.GetLoop; client.SendLoop; client.GetEnv; bs.Run]
    //[client.GetLoop; bs.Run]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
