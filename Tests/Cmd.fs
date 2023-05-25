module Elmish.Test.Tests.Cmd

open System
open Elmish
open Xunit
open Swensen.Unquote
open Elmish.Test.Core

type Msg =
    | Case1
    | Case2
    | Case3

let delayed (delayMs: int) (cmd: Cmd<'msg>) =
    let delayedCmd (dispatch: 'msg -> unit) =
        let delayedDispatch =
            async {
                do! Async.Sleep delayMs
                cmd |> List.iter (fun call -> call dispatch)
            }
        Async.Start delayedDispatch
    Cmd.ofSub delayedCmd

[<Fact>]
let ``Cmd.execute: runs all commands`` () =
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
    cmds |> Cmd.execute
    
    // Assert
    Assert.True cmd1Completed
    
[<Fact>]
let ``Cmd.Dispatches.exists: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.Dispatches.exists (fun _ -> true)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Dispatches.exists: returns true WHEN synchronous AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.exists ((=) Case2)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Dispatches.exists: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 100; Case2 |> Cmd.ofMsg |> delayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.exists ((=) Case2)
    
    // Assert
    Assert.True result
    
[<Fact>]
let ``Cmd.Dispatches.exists: returns false WHEN synchronous AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Dispatches.exists: returns false WHEN async AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 100; Case2 |> Cmd.ofMsg |> delayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Dispatches.exists: times out if running for over default three seconds`` () =
    // Arrange
    let cmd = Case1 |> Cmd.ofMsg |> delayed 3020
    
    // Act/Assert
    Assert.Throws<OperationCanceledException>(fun () ->  cmd |> Cmd.Dispatches.exists ((=) Case1) |> ignore)
    
[<Fact>]
let ``Cmd.Dispatches.exists: can increase timeout`` () =
    // Arrange
    let initialTimeout = Config.TimeoutLengthMilliseconds
    Config.TimeoutLengthMilliseconds <- initialTimeout + 200
    let cmd = Case1 |> Cmd.ofMsg |> delayed (initialTimeout + 100)
    
    // Act
    let result = cmd |> Cmd.Dispatches.exists ((=) Case1)
    
    // Assert
    Assert.True result
    
    // Cleanup
    Config.TimeoutLengthMilliseconds <- initialTimeout
    
[<Fact>]
let ``Cmd.Dispatches.exists: times out if command never dispatches`` () =
    // Arrange
    let command =
        fun _ -> ()
        |> Cmd.ofSub
    
    // Act
    // Assert
    Assert.Throws<OperationCanceledException>(fun () -> command |> Cmd.Dispatches.exists (fun _ -> true) |> ignore)
    
[<Fact>]
let ``Cmd.Dispatches.forall: returns true with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.Dispatches.forall (fun _ -> false)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Dispatches.forall: returns true WHEN synchronous AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.forall ((<>) Case3)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.Dispatches.forall: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 100; Case2 |> Cmd.ofMsg |> delayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.forall ((<>) Case3)
    
    // Assert
    Assert.True result
    
[<Fact>]
let ``Cmd.Dispatches.forall: returns false WHEN synchronous AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.forall ((=) Case2)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Dispatches.forall: returns false WHEN async AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 100; Case2 |> Cmd.ofMsg |> delayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Dispatches.forall ((=) Case2)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.Dispatches.forall: times out if running for over default configured timeout`` () =
    // Arrange
    let delay = Config.TimeoutLengthMilliseconds + 100
    let cmd = Case1 |> Cmd.ofMsg |> delayed delay
    
    // Act/Assert
    Assert.Throws<OperationCanceledException>(fun () ->  cmd |> Cmd.Dispatches.forall ((=) Case1) |> ignore)
     
[<Fact>]
let ``Cmd.Dispatches.captureMessages: captures sync messages`` () =
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
    let results = commands |> Cmd.Dispatches.captureMessages |> Set.ofList
    
    // Assert
    messages |> Set.ofList =! results
    
[<Fact>]
let ``Cmd.Dispatches.captureMessages: captures async messages`` () =
    // Arrange
    let messages =
        [
            Case1
            Case2
            Case3
        ]
        
    let commands =
        messages
        |> List.map (fun msg -> msg |> Cmd.ofMsg |> delayed 100)
        |> Cmd.batch
        
    // Act
    let results = commands |> Cmd.Dispatches.captureMessages |> Set.ofList
    
    // Assert
    messages |> Set.ofList  =! results