module DAG.Utils

open System

let BytesToString (data: byte[]) =
    Text.Encoding.UTF8.GetString data