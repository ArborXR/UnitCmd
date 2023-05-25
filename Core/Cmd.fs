namespace Elmish.Test.Core

open System
open Elmish
open System.Threading.Tasks

module Cmd =
    /// <summary>
    /// Executes the provided commands with a no-op dispatch function.
    /// </summary>
    /// <param name="cmd">Command to be executed.</param>
    /// <remarks>
    /// This function initiates the command but does not wait for its completion.
    /// If you need to wait for a command's completion that dispatches messages,
    /// refer to the <see cref="Cmd.Dispatches"/> module.
    /// Conversely, if the command does not dispatch messages and there's a need to wait for its completion,
    /// use the <see cref="Cmd.executeWithDelay"/> function.
    /// </remarks>
    let execute (cmd: Cmd<'msg>) =
        let dispatch: Dispatch<'msg> = fun _ -> ()
        cmd |> List.iter (fun call -> call dispatch)

    /// <summary>
    /// Executes the provided command after a specified delay.
    /// </summary>
    /// <param name="delay">The delay duration after the command is initiated.</param>
    /// <param name="cmd">Command to be executed.</param>
    /// <remarks>
    /// This function initiates the command, then pauses the execution for the specified delay duration.
    /// This is useful when there's a need to wait for the completion of an asynchronous command.
    /// </remarks>
    let executeWithDelay (delay: TimeSpan) (cmd: Cmd<'msg>) =
        execute cmd

        delay |> Async.Sleep |> Async.RunSynchronously

    module Dispatches =
        /// <summary>
        /// Tests whether any message dispatched by the command satisfies the given predicate.
        /// The predicate is applied to the messages dispatch by the command. If any application returns true the
        /// overall result is true and no further elements are tested. Otherwise, false is returned.
        /// </summary>
        /// <remarks>
        /// The timeout length can be configured with <see cref="Config.TimeoutLengthMilliseconds"/>.
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

                return!
                    tcs.Task
                    |> Async.AwaitTask
                    |> Async.withTimeout Config.TimeoutLengthMilliseconds
            }
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
        /// The timeout length can be configured with <see cref="Config.TimeoutLengthMilliseconds"/>.
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
            |> Async.withTimeout Config.TimeoutLengthMilliseconds
            |> Async.RunSynchronously

        /// <summary>
        /// Executes the provided commands and captures all messages sent through the dispatch function.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>A list of messages captured during the execution of the command.</returns>
        /// <remarks>
        /// The timeout length can be configured with <see cref="Config.TimeoutLengthMilliseconds"/>.
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
                            tcs.TrySetResult(msgs |> List.rev) |> ignore)

                subCmd dispatch

            async {
                cmd |> List.iter exec
                return! Async.AwaitTask tcs.Task
            }
            |> Async.withTimeout Config.TimeoutLengthMilliseconds
            |> Async.RunSynchronously
