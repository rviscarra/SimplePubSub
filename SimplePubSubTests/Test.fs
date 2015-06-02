namespace SimplePubSubTests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework

open BoW.Util

[<TestFixture>]
type PubSubTest() = 

    [<Test>]
    member x.``Test PubSub creation``() =
        let ps = PubSub.createPubSub<unit, unit>() 
        Assert.IsNotNull ps

    [<Test>]
    member __.``Test publish without subscribers``() =
        let ps = PubSub.createPubSub<unit, string>()
        ps 
        |> PubSub.publish "Hello World"

    [<Test>]
    member __.``Test addSuber and publish``() =
        let msg = "test_message"
        let ps = PubSub.createPubSub<int, string>()
        let result = ref "timeout"

        ps |> PubSub.addSuber 0 (fun msg ->
            result := msg
        )
        ps |> PubSub.publish msg
        Thread.Sleep 100
        Assert.AreEqual(msg, !result)

    [<Test>]
    member __.``Test delSuber and publish`` () = 
        let msg = "test_message"
        let ps = PubSub.createPubSub<int, string>()
        let result = ref "timeout"

        ps |> PubSub.addSuber 0 (fun msg ->
            result := msg
        )
        ps |> PubSub.delSuber 0
        ps |> PubSub.publish msg
        Thread.Sleep 200
        Assert.AreNotEqual(msg, !result)