module DAG.State

open DAG.Storage

type AppState<'T> = {
    Storage: Storage
    Listener: 'T
}
