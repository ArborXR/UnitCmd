namespace Elmish.Test.Core

open Elmish
open System.Threading.Tasks

module Cmd =
    /// <summary>
    /// Executes the provided commands with a no-op dispatch function.
    /// </summary>
    /// <param name="cmd">The command to execute.</param>
    /// <remarks>
    /// The timeout length can be configured with Config.TimeoutLength.
    /// </remarks>
    /// <exception cref="TimeoutException">
    /// Thrown if the command does not complete within the specified time.
    /// </exception>
    let execute (cmd: Cmd<'msg>) =
        let tcs = TaskCompletionSource<unit>()
        let mutable pending = cmd.Length
        let syncRoot = obj ()

        let exec subCmd =
            let dispatch _ =
                lock syncRoot (fun () ->
                    pending <- pending - 1

                    if pending = 0 then
                        tcs.TrySetResult() |> ignore)

            subCmd dispatch

        async {
            cmd |> List.iter exec
            return! Async.AwaitTask tcs.Task
        }
        |> Async.withTimeout Config.TimeoutLength
        |> Async.RunSynchronously

    /// <summary>
    /// Tests whether any message dispatched by the command satisfies the given predicate.
    /// The predicate is applied to the messages dispatch by the command. If any application returns true the
    /// overall result is true and no further elements are tested. Otherwise, false is returned.
    /// </summary>
    /// <remarks>
    /// The timeout length can be configured with Config.TimeoutLength.
    /// </remarks>
    /// <param name="predicate">The function to test the dispatched messages.</param>
    /// <param name="cmd">The input Cmd.</param>
    /// <returns>True if any element satisfies the predicate. Otherwise false.</returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the command does not complete within the specified time.
    /// </exception>
    let exists (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
        let tcs = TaskCompletionSource<bool>()
        let mutable pending = cmd.Length
        let syncRoot = obj ()

        if pending = 0 then
            tcs.TrySetResult false |> ignore

        let exec subCmd =
            let dispatch (msg: 'msg) =
                lock syncRoot (fun () ->
                    pending <- pending - 1

                    if not tcs.Task.IsCompleted then
                        if predicate msg then
                            tcs.TrySetResult true |> ignore
                        else if pending = 0 then
                            tcs.TrySetResult false |> ignore)

            subCmd dispatch

        async {
            cmd |> List.iter exec
            return! Async.AwaitTask tcs.Task
        }
        |> Async.withTimeout Config.TimeoutLength
        |> Async.RunSynchronously

    /// <summary>
    /// Tests whether all messages dispatched by the command satisfies the given predicate.
    /// The predicate is applied to the messages dispatch by the command. If any application returns false then the
    /// overall result is false and no further elements are tested. Otherwise, true is returned.
    /// </summary>
    /// <param name="predicate">The function to test the dispatched messages.</param>
    /// <param name="cmd">The input Cmd.</param>
    /// <returns>True if every element satisfies the predicate. Otherwise false.</returns>
    /// <remarks>
    /// The timeout length can be configured with Config.TimeoutLength.
    /// </remarks>
    /// <exception cref="TimeoutException">
    /// Thrown if the command does not complete within the specified time.
    /// </exception>
    let forall (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
        let tcs = TaskCompletionSource<bool>()
        let mutable pending = cmd.Length

        if pending = 0 then
            tcs.TrySetResult true |> ignore

        let syncRoot = obj ()

        let exec subCmd =
            let dispatch (msg: 'msg) =
                lock syncRoot (fun () ->
                    pending <- pending - 1

                    if not tcs.Task.IsCompleted then
                        if msg |> predicate |> not then
                            tcs.TrySetResult false |> ignore
                        else if pending = 0 then
                            tcs.TrySetResult true |> ignore)

            subCmd dispatch

        async {
            cmd |> List.iter exec
            return! Async.AwaitTask tcs.Task
        }
        |> Async.withTimeout Config.TimeoutLength
        |> Async.RunSynchronously

    /// <summary>
    /// Executes the provided commands and captures all messages sent through the dispatch function.
    /// </summary>
    /// <param name="cmd">The command to execute.</param>
    /// <returns>A list of messages captured during the execution of the command.</returns>
    /// <remarks>
    /// The timeout length can be configured with Config.TimeoutLength.
    /// </remarks>
    /// <exception cref="TimeoutException">
    /// Thrown if the command does not complete within the specified time.
    /// </exception>
    let captureMessages (cmd: Cmd<'msg>) =
        let tcs = TaskCompletionSource<'msg list>()
        let mutable pending = cmd.Length
        let mutable msgs = []
        let syncRoot = obj ()

        let exec subCmd =
            let dispatch msg =
                lock syncRoot (fun () ->
                    msgs <- msg :: msgs
                    pending <- pending - 1

                    if pending = 0 then
                        tcs.TrySetResult msgs |> ignore)

            subCmd dispatch

        async {
            cmd |> List.iter exec
            return! Async.AwaitTask tcs.Task
        }
        |> Async.withTimeout Config.TimeoutLength
        |> Async.RunSynchronously
