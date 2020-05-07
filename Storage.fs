module DAG.Storage

open System
open System.Diagnostics
open System.IO
open System.Text
open RocksDbSharp

type IntPtr with
    member this.IsError() =
        IntPtr.Zero = this

type StorageErrors<'T> =
    | Error of IntPtr
    | Result of 'T
    | None
    member this.isError() =
        match this with
        | None -> false
        | Result _ -> false
        | _ -> true
    static member Return(err: IntPtr) =
        if err = IntPtr.Zero then
            StorageErrors.None
        else
            StorageErrors.Error err
    static member Return<'T>(result: 'T, err: IntPtr) =
        if err = IntPtr.Zero then
            StorageErrors.Result result
        else
            StorageErrors.Error err

type Storage(path: string) =
    let DBPath = Environment.ExpandEnvironmentVariables(Path.Combine(Directory.GetCurrentDirectory(), path))
    let options = Native.Instance.rocksdb_options_create()
    let writeoptions = Native.Instance.rocksdb_writeoptions_create()
    let wb = Native.Instance.rocksdb_writebatch_create()
    let readoptions = Native.Instance.rocksdb_readoptions_create()
    let mutable db =
        let err = IntPtr.Zero
        let db = Native.Instance.rocksdb_open(options, DBPath, ref err)
        Debug.Assert(err.IsError())
        Native.Instance.rocksdb_options_increase_parallelism(options, Environment.ProcessorCount)
        Native.Instance.rocksdb_options_optimize_level_style_compaction(options, 0UL)
        Native.Instance.rocksdb_options_set_create_if_missing(options, true)
        db
    member this.Set(key, value) =
        let err = IntPtr.Zero
        Native.Instance.rocksdb_put(db, writeoptions, key, value, ref err)
        StorageErrors<_>.Return(err)
    member this.SetMany(key, value) =
        Native.Instance.rocksdb_writebatch_put(writeoptions, key, value, Encoding.UTF8)
        this
    member this.Flush() =
        Native.Instance.rocksdb_write(db, writeoptions, wb)
    member this.Get(key) =
        let err = IntPtr.Zero
        let value = Native.Instance.rocksdb_get(db, readoptions, key, ref err)
        StorageErrors<_>.Return(value, err)        

let connect =
    let storage = Storage("test")
    let res = storage.Set("key1", "123")
    printfn "Set: %A" res
    let res = storage.Set("key2", "124")
    printfn "Set: %A" res
    let res = storage.Get("key1")
    printfn "Get: %A" res
    let res = storage.Get("key2")
    printfn "Get: %A" res
    
    