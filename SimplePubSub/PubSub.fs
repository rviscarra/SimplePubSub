module BoW.Util.PubSub

type PublishCallback<'Id, 'Msg> = 
    'Id -> 'Msg -> unit

type internal PubSubCommand<'Id, 'Msg when 'Id : comparison> =
    | AddSuber of 'Id * PublishCallback<'Id, 'Msg>
    | DelSuber of 'Id
    | Publish  of 'Msg
    | Stop

type PubSub<'Id, 'Msg when 'Id : comparison> = internal {
    Actor : MailboxProcessor<PubSubCommand<'Id, 'Msg>>
} 

type private PubSubState<'Id, 'Msg when 'Id : comparison> = 
    | PubSubState of Map<'Id, PublishCallback<'Id, 'Msg>>

let publishToAll msg subMap =
    subMap
    |> Map.fold(fun acc k pubFn ->
        async {
            pubFn k msg
        } :: acc
    ) List.empty
    |> Async.Parallel
    |> Async.Ignore

let createPubSub<'Id, 'Msg when 'Id : comparison> () = 
    let actor = 
        MailboxProcessor.Start(fun mb ->
            let rec loop (st : PubSubState<'Id, 'Msg>) =
                let (PubSubState subMap) = st 
                async {
                    let! msg = mb.Receive()
                    match msg with 
                    | AddSuber (id, cb) ->
                        return! loop <| PubSubState (Map.add id cb subMap)
                    | DelSuber id ->
                        return! loop <| PubSubState (Map.remove id subMap)
                    | Publish msg ->
                        do! publishToAll msg subMap
                        return! loop <| PubSubState subMap
                    | Stop ->
                        return ()
                }
            loop <| PubSubState Map.empty<'Id, PublishCallback<'Id, 'Msg>>
        )
    { Actor = actor }

let addSuber id cb ps =
    ps.Actor.Post <| AddSuber (id ,cb)

let delSuber id ps =
    ps.Actor.Post <| DelSuber id

let publish msg ps = 
    ps.Actor.Post <| Publish msg

let stop ps =
    ps.Actor.Post Stop
