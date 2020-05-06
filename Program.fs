// Learn more about F# at http://fsharp.org

open DAG
open Storage

[<EntryPoint>]
let main argv =
    Storage.connect
    printfn "###"
    
    let client = Network.UdpConnect("127.0.0.1", 3000)
    [client.GetLoop; client.SendLoop]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
