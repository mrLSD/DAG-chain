/// DAG chain implementation

open DAG

[<EntryPoint>]
let main argv =
    let client  = new Network.UdpConnect("127.0.0.1", 3000)
    [client.GetLoop; client.SendLoop; client.GetEnv]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
