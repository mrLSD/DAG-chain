module DAG.Storage

open System
open System.Diagnostics
open System.IO
open RocksDbSharp

let connect =
    let DBPath = Environment.ExpandEnvironmentVariables(Path.Combine(Directory.GetCurrentDirectory(), "data"))
    let DBBackupPath = Environment.ExpandEnvironmentVariables(Path.Combine(Directory.GetCurrentDirectory(), "backup"))
    let options = DbOptions()
                    .SetCreateIfMissing(true)
                    .EnableStatistics()

    let options = Native.Instance.rocksdb_options_create()
    let cpus = Environment.ProcessorCount
    Native.Instance.rocksdb_options_increase_parallelism(options, cpus)
    Native.Instance.rocksdb_options_optimize_level_style_compaction(options, 0)
    Native.Instance.rocksdb_options_set_create_if_missing(options, true);

    let err = IntPtr.Zero;
    let db = Native.Instance.rocksdb_open(options, DBPath, ref err)
    Debug.Assert((err = IntPtr.Zero));

    // open Backup Engine that we will use for backing up our database
    let be = Native.Instance.rocksdb_backup_engine_open(options, DBBackupPath, ref err);
    Debug.Assert((err = IntPtr.Zero));

    // Put key-value
    let writeoptions = Native.Instance.rocksdb_writeoptions_create();
    let key = "key";
    let value = "value"
    Native.Instance.rocksdb_put(db, writeoptions, key, value, ref err)
    Debug.Assert((err = IntPtr.Zero));
    let readoptions = Native.Instance.rocksdb_readoptions_create();
    let returned_value = Native.Instance.rocksdb_get(db, readoptions, key, ref err);
    Debug.Assert((err = IntPtr.Zero));
    Debug.Assert((returned_value = "value"));
//    let db = RocksDb.Open(options, path)
//    let column = db.CreateColumnFamily(ColumnFamilyOptions(), "reverse")
//    db.Put("key", "val", column)
//    let res = db.Get("key", column)
//    printfn "val: %A" res
//    let res = db.Get("key")
//    printfn "val: %A" res