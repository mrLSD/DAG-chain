module DAG.Utils

open System

let BytesToString (data: byte[]) =
    Text.Encoding.UTF8.GetString data

let BytesToStringLength (data: byte[], len: int) =
    Text.Encoding.UTF8.GetString(data, 0, len)
    
let StringToBytes (data: string) =
    Text.Encoding.UTF8.GetBytes data
