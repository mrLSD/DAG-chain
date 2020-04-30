// Learn more about F# at http://fsharp.org

open System

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

[<EntryPoint>]
let main argv =
    let client = UdpConnect("127.0.0.1", 3000)
    [client.GetLoop; client.SendLoop]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    0 // return an integer exit code
