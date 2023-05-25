# Elmish.Test
Elmish.Test is a library created to facilitate unit testing with Elmish commands in F#. The library allows you to run commands, capture dispatched messages, or make assertions against the dispatched messages. 

## Getting Started

To use Elmish.Test, you need to add a reference to it in your F# project. You can add this through NuGet or reference it directly if you have cloned the source code.

Once Elmish.Test is referenced in your project, you can import it in your test files:

```fsharp
open Elmish.Test.Core
open Elmish.Test.NUnitExt
```

## Elmish.Test.Core
Contains core functions to run commands, capture messages, etc.. Is used by other projects, such as `NUnitExt`.

`Cmd.execute (cmd: Cmd<'msg>): unit` - Executes the provided command with a no-op dispatch function. This is useful when there's a need to initiate a command but not wait for its completion.

`Cmd.executeWithDelay (delay: TimeSpan) (cmd: Cmd<'msg>): unit` - Executes the provided command after a specified delay. It can be used when there's a need to wait for the completion of an asynchronous command. Note that this is different from Cmd.execute in that it waits for a specified delay after initiating the command.

`Cmd.Dispatches.exists (predicate: 'msg -> bool) (cmd: Cmd<'msg>): bool` - Determines if any message dispatched by the provided command satisfies the given predicate function.

`Cmd.Dispatches.forall (predicate: 'msg -> bool) (cmd: Cmd<'msg>): bool` - Determines whether all messages dispatched by the provided command satisfy the given predicate function.

`Cmd.Dispatches.captureMessages (cmd: Cmd<'msg>): 'msg list` - Captures all messages dispatched by the provided command. The order of the captured messages in the result list corresponds to the order of dispatches in the command.

## Elmish.Test.NUnitExt
Extends NUnit's `Assert` module with functions corresponding to the `Cmd.Dispatches.exists` and `Cmd.Dispatches.forall` functions. 

`Assert.Cmd.exists (predicate: 'msg -> bool) (cmd: Cmd<'msg>): unit` - Asserts that at least one message dispatched by the provided command satisfies the given predicate function. It uses `Cmd.Dispatches.exists` to check if any message satisfies the predicate. If not, it throws an `AssertionException`.

`Assert.Cmd.forall (predicate: 'msg -> bool) (cmd: Cmd<'msg>)` - Asserts that all messages dispatched by the provided command satisfy the given predicate function. It uses `Cmd.Dispatches.forall` to check if all messages satisfy the predicate. If not, it throws an `AssertionException`.

## Gotchas
- All functions in the `Core.Cmd.Dispatches` module (e.g., `Cmd.Dispatches.exists`) requires that each command invokes its dispatch function. If a command fails to do so within the configured timeout period, the function will timeout and throw an `OperationCanceledException`. The same holds for any function that depends on these functions (e.g., `Assert.Cmd.exists`).
- When using `Cmd.execute`, the command gets initiated, but the function doesn't wait for its completion. If you need to wait for a command's completion that dispatches messages, refer to the `Cmd.Dispatches` module. Conversely, if the command does not dispatch messages and there's a need to wait for its completion, use `Cmd.executeWithDelay`.


## License
This project is licensed under the Mozilla Public License 2.0 (MPL 2.0). See [LICENSE](https://github.com/bryanbharper/Elmish.Test/blob/main/LICENSE) for details.
