// Learn more about F# at http://fsharp.org

open System
open DAG
open DAG.Log

type UdpState = {
    udpClient: Net.Sockets.UdpClient
    endPoint: Net.IPEndPoint
}

type UdpConnect(addr: string, port: int) =
    let udpClient = new Net.Sockets.UdpClient()
    let recvUdpClient = new Net.Sockets.UdpClient(port)
    do
        udpClient.Connect(addr, port)
    member this.Send(data: byte[]) = async {
        printfn "<- Send message: %A" (Text.Encoding.ASCII.GetString data)
        return! udpClient.SendAsync(data, data.Length) |> Async.AwaitTask
    }
    member this.Get() = async {
        printfn "Wait..."
        let! msg = recvUdpClient.ReceiveAsync() |> Async.AwaitTask
        let msgStr = Text.Encoding.ASCII.GetString msg.Buffer
        printfn "-> Get message: %s" msgStr
        return msg
    }
    member this.SendLoop = async {
        let msg = sprintf "\tTime: %O" DateTime.Now.TimeOfDay
        do! this.Send (Text.Encoding.ASCII.GetBytes msg) |> Async.Ignore
        do! Async.Sleep 1000
        do! this.SendLoop
    }
    member this.GetLoop = async {
        do! this.Get() |> Async.Ignore
        do! this.GetLoop
    }

type EventTypes =
    | Type1
    | Type2
    | Type3 of string

type EventS = {
    EvType: EventTypes
    X: String
}

type EventClass() =
    let myEvent = Event<EventS>()
    [<CLIEvent>]
    member this.MyEvent = myEvent.Publish
    member this.AddEvent(evType: EventTypes, fn: EventS -> unit ) =
        this.MyEvent
            |> Event.filter (fun (e: EventS) -> e.EvType.Equals evType)
            |> Event.add fn
    member this.Send(arg) =
        myEvent.Trigger(arg)
    member this.Listener() =
        this.MyEvent.Add(fun e -> printfn "EventListener: %A" e)
        Threading.Thread.Sleep 1000

[<EntryPoint>]
let main argv =
    do Log.debug {MyT.X = 10; Y = "The Y"}
    let e = EventClass()
    e.AddEvent(EventTypes.Type1, fun e -> printfn "Event1: %A" e)
    e.AddEvent(EventTypes.Type2, fun e -> printfn "Event2: %A" e)
    e.Listener()
    e.Send({
        X = sprintf "\t# %O" DateTime.Now.TimeOfDay
        EvType = EventTypes.Type1
    })
    e.Send({
        X = sprintf "\t# %O" DateTime.Now.TimeOfDay
        EvType = EventTypes.Type2
    })
    
    let client = UdpConnect("127.0.0.1", 3000)
    [client.GetLoop; client.SendLoop]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
