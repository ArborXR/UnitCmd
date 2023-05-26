module Elmish.Test.Tests.Cmd

open System
open System.Threading
open Elmish
open Xunit
open Swensen.Unquote
open Elmish.Test.Core

type Msg =
    | Case1
    | Case2
    | Case3

module Cmd =
    let ofMsgDelayed (delayMs: int) (msg: Msg) =
        let sub (dispatch: Msg -> unit) : unit =
            let delayedDispatch = async {
                do! Async.Sleep delayMs
                dispatch msg
            }

            Async.StartImmediate delayedDispatch

        Cmd.ofSub sub

[<Fact>]
let ``Cmd.start: runs all commands`` () =
    // Arrange
    let mutable cmd1Completed = false
    let mutable cmd2Completed = false
    
    let cmd1 =
        fun _ -> cmd1Completed <- true
        |> Cmd.ofSub
    
    let cmd2 =
        fun _ -> cmd2Completed <- true
        |> Cmd.ofSub
    
    let cmds =
        [
            cmd1
            cmd2
        ]
        |> Cmd.batch
    
    // Act
    cmds |> Cmd.start
    
    // Assert
    Assert.True cmd1Completed
 
[<Fact>]
let ``Cmd.exists: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.exists (fun _ -> true)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.exists: returns true WHEN predicate satisfied by at least one message`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case2)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.exists: returns false WHEN predicate is not satisfied by any message`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.forall: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.forall (fun _ -> false)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.forall: returns true WHEN all messages satisfy predicate`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.forall ((<>) Case3)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.forall: returns false WHEN one message does not satisfy predicate`` () =
    // Arrange
    let cmd =
        [ Cmd.ofMsg Case1; Cmd.ofMsg Case1;  Cmd.ofMsg Case2 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.forall ((=) Case1)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.captureMessages: captures messages`` () =
    // Arrange
    let messages =
        [
            Case1
            Case2
            Case3
        ]
        
    let commands =
        messages
        |> List.map (fun msg -> msg |> Cmd.ofMsg)
        |> Cmd.batch
        
    // Act
    let results = commands |> Cmd.captureMessages |> Set.ofList
    
    // Assert
    messages |> Set.ofList  =! results

[<Fact>]
let ``Cmd.Await.exists: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.Await.exists (fun _ -> true)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Await.exists: returns true WHEN synchronous AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.exists ((=) Case2)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Await.exists: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.exists ((=) Case2)
    
    // Assert
    Assert.True result
    
[<Fact>]
let ``Cmd.Await.exists: returns false WHEN synchronous AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Await.exists: returns false WHEN async AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Await.exists: times out if running for over default three seconds`` () =
    // Arrange
    let cmd = Case1 |> Cmd.ofMsgDelayed 3020
    
    // Act/Assert
    Assert.Throws<OperationCanceledException>(fun () ->  cmd |> Cmd.Await.exists ((=) Case1) |> ignore)
    
[<Fact>]
let ``Cmd.Await.exists: can increase timeout`` () =
    // Arrange
    let initialTimeout = Config.TimeoutLengthMilliseconds
    Config.TimeoutLengthMilliseconds <- initialTimeout + 200
    let cmd = Case1 |> Cmd.ofMsgDelayed (initialTimeout + 100)
    
    // Act
    let result = cmd |> Cmd.Await.exists ((=) Case1)
    
    // Assert
    Assert.True result
    
    // Cleanup
    Config.TimeoutLengthMilliseconds <- initialTimeout
    
[<Fact>]
let ``Cmd.Await.exists: times out if command never dispatches`` () =
    // Arrange
    let command =
        fun _ -> ()
        |> Cmd.ofSub
    
    // Act
    // Assert
    Assert.Throws<OperationCanceledException>(fun () -> command |> Cmd.Await.exists (fun _ -> true) |> ignore)
    
[<Fact>]
let ``Cmd.Await.forall: returns true with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.Await.forall (fun _ -> false)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Await.forall: returns true WHEN synchronous AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.forall ((<>) Case3)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Await.forall: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.forall ((<>) Case3)
    
    // Assert
    Assert.True result
    
[<Fact>]
let ``Cmd.Await.forall: returns false WHEN synchronous AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.forall ((=) Case2)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Await.forall: returns false WHEN async AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.forall ((=) Case2)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Await.forall: times out if running for over default configured timeout`` () =
    // Arrange
    let delay = Config.TimeoutLengthMilliseconds + 100
    let cmd = Case1 |> Cmd.ofMsgDelayed delay
    
    // Act/Assert
    Assert.Throws<OperationCanceledException>(fun () ->  cmd |> Cmd.Await.forall ((=) Case1) |> ignore)
     
[<Fact>]
let ``Cmd.Await.captureMessages: captures sync messages`` () =
    // Arrange
    let messages =
        [
            Case1
            Case2
            Case3
        ]
        
    let commands =
        messages
        |> List.map Cmd.ofMsg
        |> Cmd.batch
        
    // Act
    let results = commands |> Cmd.Await.captureMessages |> Set.ofList
    
    // Assert
    messages |> Set.ofList =! results
    
[<Fact>]
let ``Cmd.Await.captureMessages: captures async messages`` () =
    // Arrange
    let messages =
        [
            Case1
            Case2
            Case3
        ]
        
    let commands =
        messages
        |> List.map (fun msg -> msg |> Cmd.ofMsgDelayed 100)
        |> Cmd.batch
        
    // Act
    let results = commands |> Cmd.Await.captureMessages |> Set.ofList
    
    // Assert
    messages |> Set.ofList  =! results
    
[<Fact>]
let ``Cmd.Delay.start: runs all commands`` () =
    // Arrange
    let mutable cmd1Completed = false
    let mutable cmd2Completed = false
    
    let cmd1 =
        fun _ ->
            async {
                do! Async.Sleep 100
                cmd1Completed <- true
            }
            |> Async.StartImmediate
        |> Cmd.ofSub

    
    let cmd2 =
        fun _ ->
            async {
                do! Async.Sleep 100
                cmd2Completed <- true
            }
            |> Async.StartImmediate
        |> Cmd.ofSub
        
    let cmds =
        [
            cmd1
            cmd2
        ]
        |> Cmd.batch
    
    // Act
    cmds |> Cmd.Delay.start (TimeSpan.FromMilliseconds 300)
    
    // Assert
    Assert.True cmd1Completed
    Assert.True cmd2Completed
 
[<Fact>]
let ``Cmd.Delay.exists: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.Delay.exists (TimeSpan.FromMilliseconds 150) (fun _ -> true)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Delay.exists: returns true WHEN predicate satisfied by at least one message`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Delay.exists (TimeSpan.FromMilliseconds 150) ((=) Case2)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Delay.exists: returns false WHEN predicate is not satisfied by any message`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Delay.exists (TimeSpan.FromMilliseconds 150) ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Delay.forall: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.Delay.forall (TimeSpan.FromMilliseconds 150) (fun _ -> false)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Delay.forall: returns true WHEN all messages satisfy predicate`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Delay.forall (TimeSpan.FromMilliseconds 150) ((<>) Case3)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Delay.forall: returns false WHEN one message does not satisfy predicate`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsgDelayed 100; Case1 |> Cmd.ofMsgDelayed 100;  Case2 |> Cmd.ofMsgDelayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Delay.forall (TimeSpan.FromMilliseconds 150) ((=) Case1)
    
    // Assert
    Assert.False result
    
[<Fact>]
let ``Cmd.Delay.captureMessages: captures messages`` () =
    // Arrange
    let messages =
        [
            Case1
            Case2
            Case3
        ]
        
    let ofMsgDelayed (delayMs: int) (msg: Msg) =
        let sub (dispatch: Msg -> unit) : unit =
            Thread.Sleep delayMs
            dispatch msg
        Cmd.ofSub sub

    let commands =
        messages
        |> List.map (ofMsgDelayed 100)
        |> Cmd.batch
        
    // Act
    let results = commands |> Cmd.Delay.captureMessages (TimeSpan.FromMilliseconds 400)
    
    // Assert
    messages |> Set.ofList  =! (results |> Set.ofList)
    