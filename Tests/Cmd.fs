module Elmish.Test.Tests.Cmd

open System
open Elmish
open Xunit
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
let ``Cmd.exists: returns false with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.exists (fun _ -> true)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.exists: returns true WHEN synchronous AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case2)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.exists: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 500; Case2 |> Cmd.ofMsg |> delayed 500 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case2)
    
    // Assert
    Assert.True result
    
[<Fact>]
let ``Cmd.exists: returns false WHEN synchronous AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.exists: returns false WHEN async AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 500; Case2 |> Cmd.ofMsg |> delayed 500 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case3)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.exists: times out if running for over default three seconds`` () =
    // Arrange
    let cmd = Case1 |> Cmd.ofMsg |> delayed 3020
    
    let shouldThrow = Action(fun () ->  cmd |> Cmd.exists ((=) Case1) |> ignore)
    
    // Act/Assert
    Assert.Throws<TimeoutException>(shouldThrow)
    
[<Fact>]
let ``Cmd.exists: can increase timeout`` () =
    // Arrange
    Config.TimeoutLengthMilliseconds <- 4000
    let cmd = Case1 |> Cmd.ofMsg |> delayed 3020
    
    // Act
    let result = cmd |> Cmd.exists ((=) Case1)
    
    // Assert
    Assert.True result
    
    // Cleanup
    Config.TimeoutLengthMilliseconds <- 3000
    
[<Fact>]
let ``Cmd.forall: returns true with Cmd.none`` () =
    // Arrange
    let cmd = Cmd.none
    
    // Act
    let result = cmd |> Cmd.forall (fun _ -> false)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.forall: returns true WHEN synchronous AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.forall ((<>) Case3)
    
    // Assert
    Assert.True result

[<Fact>]
let ``Cmd.forall: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 500; Case2 |> Cmd.ofMsg |> delayed 500 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.forall ((<>) Case3)
    
    // Assert
    Assert.True result
    
[<Fact>]
let ``Cmd.forall: returns false WHEN synchronous AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg; Case2 |> Cmd.ofMsg ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.forall ((=) Case2)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.forall: returns false WHEN async AND predicate is not satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 500; Case2 |> Cmd.ofMsg |> delayed 500 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.forall ((=) Case2)
    
    // Assert
    Assert.False result

[<Fact>]
let ``Cmd.forall: times out if running for over default three seconds`` () =
    // Arrange
    let cmd = Case1 |> Cmd.ofMsg |> delayed 3020
    
    let shouldThrow = Action(fun () ->  cmd |> Cmd.forall ((=) Case1) |> ignore)
    
    // Act/Assert
    Assert.Throws<TimeoutException>(shouldThrow)
    
[<Fact>]
let ``Cmd.forall: can increase timeout`` () =
    // Arrange
    Config.TimeoutLengthMilliseconds <- 4000
    let cmd = Case1 |> Cmd.ofMsg |> delayed 3020
    
    // Act
    let result = cmd |> Cmd.forall ((=) Case1)
    
    // Assert
    Assert.True result
    
    // Cleanup
    Config.TimeoutLengthMilliseconds <- 3000