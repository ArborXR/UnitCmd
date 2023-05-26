# Elmish.Test
Elmish.Test is a library created to facilitate unit testing with Elmish commands in F#. The library allows you to run commands, capture dispatched messages, or make assertions against the dispatched messages. 

# Getting Started

To Do: Nuget command, link, etc.

## Basic (Synchronous) Use

The `Cmd.start` function can be used to run commands, which can be used to verify that a specific _(synchronous)_
command has been returned by your Elmish `update` function.

```fsharp
open Elmish.Test.Core

[<Fact>]
let ``Cmd.start: runs all commands`` () =
    // Arrange
    let mutable completed = false
    
    let cmd =
        fun _ -> completed <- true
        |> Cmd.ofSub
    
    // Act
    cmd |> Cmd.start
    
    // Assert
    Assert.True completed
```

The `Cmd.captureMessages` function can be used to capture all messages dispatched by a (synchronous) command. 

```fsharp
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
    let results = commands |> Cmd.captureMessages 
    
    // Assert
    (messages |> Set.ofList) = (results |> Set.ofList) 
    |> Assert.True
```

The `Cmd.exists` and `Cmd.forall` functions operate just like the `Seq.exists` and `Seq.forall` functions, 
but with `Cmd<'msg>`s rather than `Seq<'a>`s.
```fsharp
[<Fact>]
let ``Cmd.exists: returns true WHEN predicate satisfied by at least one message`` () =
    // Arrange
    let cmd =
        [ Cmd.ofMsg Case1; Cmd.ofMsg Case2; Cmd.ofMsg Case3 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.exists ((=) Case2)
    
    // Assert
    Assert.True result
```

> **IMPORTANT:** These functions initiate the provided commands but do not wait for the completion of any asynchronous 
> sub-commands. If you need to wait for the completion of an asynchronous command, see the `Await` and `Delay` 
> sections below.

## The `Cmd.Await` Module
The functions listed above do not wait for the completion of asynchronous commands. Thus, if you have an asynchronous 
command, the test may finish before the command does, causing a false positive/negative. 

The `Cmd.Await` module has the same functions as before (`start`, `captureMessages`, `exists`, and `forall`), with the 
key difference that these functions will wait for the `dispatch` function to get invoked by _every sub-command_ provided.
This enables you test test asynchronous commands that dispatch messages.

```fsharp
[<Fact>]
let ``Cmd.Await.exists: returns true WHEN async AND predicate satisfied`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 100; Case2 |> Cmd.ofMsg |> delayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Await.exists ((=) Case2)
    
    // Assert
    Assert.True result
```

There is an _important gotcha_, however. If _any_ sub command does not invoke its `dispatch` function, the
test will timeout:

```fsharp
[<Fact>]
let ``Cmd.Await.exists: times out if command never dispatches`` () =
    // Arrange
    let command =
        fun _ -> ()
        |> Cmd.ofSub
    
    // Act
    // Assert
    Assert.Throws<OperationCanceledException>(fun () -> command |> Cmd.Await.exists (fun _ -> true) |> ignore)
```

## The `Cmd.Delay` Module

The `Cmd.Delay` module is a middle ground between the above modules. It has the same functions before, except each has
an additional `(delay: TimeSpan)` parameter. Instead of waiting for the `dispatch` function to get invoked, these
functions start the command and then wait for the specified amount of time before completing.

```fsharp
[<Fact>]
let ``Cmd.Delay.exists: returns true WHEN predicate satisfied by at least one message`` () =
    // Arrange
    let cmd =
        [ Case1 |> Cmd.ofMsg |> delayed 100; Case2 |> Cmd.ofMsg |> delayed 100 ]
        |> Cmd.batch

    // Act
    let result = cmd |> Cmd.Delay.exists (TimeSpan.FromMilliseconds 150) ((=) Case2)
    
    // Assert
    Assert.True result
```

The upshot is that these functions can handle commands which do not make use of their dispatch function. The downside
is that it requires a good estimate of how long your asynchronous commands will run. These functions also make
use of `Thread.Sleep` and so should be avoided if possible.

# Configuration
The length of time before a function times out can be configured.

```fsharp
open Elmish.Tests.Core

Config.TimeoutLengthMilliseconds <- 3000
```

The default value is 2000 milliseconds. 
