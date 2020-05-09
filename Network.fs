module DAG.Network

open System
open DAG.Log
open DAG.Utils

type EventNetwork =
    | GetMessage of byte[]
    | SendMessage of byte[]

type UdpConnect(addr: string, port: int) =
    let udpClient = new Net.Sockets.UdpClient()
    let recvUdpClient = new Net.Sockets.UdpClient(port)
    do
        udpClient.Connect(addr, port)
    let networkEvent = Event<EventNetwork>()
    member this.NetworkEvent = networkEvent.Publish
    member this.Send(data: byte[]) = async {
        Logger.Debug("<- Send message: {msg}", BytesToString data)
        networkEvent.Trigger(EventNetwork.SendMessage data)
        return! udpClient.SendAsync(data, data.Length) |> Async.AwaitTask
    }
    member this.Get() = async {
        let! msg = recvUdpClient.ReceiveAsync() |> Async.AwaitTask
        Logger.Debug("-> Get message: {msg}", BytesToString msg.Buffer)
        networkEvent.Trigger(EventNetwork.GetMessage msg.Buffer)
        return msg
    }
    member this.Subscribe(e: EventNetwork, handler: EventNetwork -> unit) =
        this.NetworkEvent
            |> Event.filter (fun (en: EventNetwork) -> e.Equals(en) )
            |> Event.add handler
    member this.SendLoop = async {
        let msg = sprintf "Время: %O" DateTime.Now.TimeOfDay
        do! this.Send (Text.Encoding.UTF8.GetBytes msg) |> Async.Ignore
        do! Async.Sleep 1000
        do! this.SendLoop
    }
    member this.GetLoop = async {
        do! this.Get() |> Async.Ignore
        do! this.GetLoop
    }

type NodeBootsrap(port: int) =
    let client = UdpConnect("0.0.0.0", port)
    member this.Run() =
        this.Run()