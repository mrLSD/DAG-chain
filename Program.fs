// Learn more about F# at http://fsharp.org

open System

type UdpState = {
    udpClient: Net.Sockets.UdpClient
    endPoint: Net.IPEndPoint
}

type UdpConnect(addr: string, port: int) =
    let udpClient = new Net.Sockets.UdpClient()
    let endPoint = new Net.IPEndPoint(Net.IPAddress.Any, port)
    do
        udpClient.Connect(addr, port)
    member this.Send(data) =
        udpClient.Send(data, data.Length)
    member private this.ReceiveCallback(ar: IAsyncResult) =
        let state = ar.AsyncState :?> UdpState
        let receivedBytes = state.udpClient.EndReceive(ar, ref state.endPoint)
        let receivedString = Text.Encoding.ASCII.GetString receivedBytes
        printfn "ReceiveCallback: %s" receivedString 
    member this.ReceiveMessages() =
        let endPoint = Net.IPEndPoint(Net.IPAddress.Any, port)
        let udpClient = new Net.Sockets.UdpClient(endPoint)
        let state: UdpState = {
            endPoint = endPoint
            udpClient = udpClient 
        }
        printfn "listening for messages"
        udpClient.BeginReceive(AsyncCallback this.ReceiveCallback, state)
let mainConnect =
    let sendBytes = Text.Encoding.ASCII.GetBytes("PING");
    let udpClient = UdpConnect("127.0.0.1", 11000)
    udpClient.Send sendBytes 

[<EntryPoint>]
let main argv =
    printfn "F# UDP!"
    let _ = mainConnect
    
    0 // return an integer exit code
