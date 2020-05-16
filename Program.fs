/// DAG chain implementation

open DAG
open FSharp.Json

type Tst1<'T> = {
    LoLo1: int
    LoLo2: string
    LoLo3: 'T
}

[<EntryPoint>]
let main argv =
    let data1 = {
        Tst1.LoLo1 = 10
        LoLo2 = "tst"
        LoLo3 = 10.1
    }
    let data2 = {
        Tst1.LoLo1 = 10
        LoLo2 = "tst"
        LoLo3 = "tst-3"
    }
    //let config = JsonConfig.create(jsonFieldNaming = Json.snakeCase)
    let j1 = Json.serialize data1
    let j2 = Json.serialize data2
    printfn "%A" j1
    printfn "%A" j2
    
    let d1 = Json.deserialize<Tst1<float>> j1
    let d2: Tst1<string> = Json.deserialize j2
    printfn "%A" d1
    printfn "%A\n\n==============" d2    
    
    let client = new Network.UdpConnect("127.0.0.1", 3000)
    [client.GetLoop; client.SendLoop]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
