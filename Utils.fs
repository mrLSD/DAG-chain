module DAG.Utils

open System

let BytesToString (data: byte[]) =
    Text.Encoding.UTF8.GetString data
    
let StringToBytes (data: string) =
    Text.Encoding.UTF8.GetBytes data
