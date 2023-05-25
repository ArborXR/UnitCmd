namespace Elmish.Test.Core

open System
open System.Threading
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
        /// Determines if any message dispatched by the provided command satisfies the given predicate function.
        /// </summary>
        /// <remarks>
        /// This function requires that each command invokes its dispatch function. If a command fails to do so within
        /// the configured timeout period, the function will timeout and throw an OperationCanceledException.
        /// The timeout duration can be adjusted with <see cref="Config.TimeoutLengthMilliseconds"/>.
        /// </remarks>
        /// <param name="predicate">
        /// A function that tests whether a given message fulfills a specific condition.
        /// </param>
        /// <param name="cmd">The command (Cmd) that dispatches messages to be tested by the predicate function.</param>
        /// <returns>
        /// True if at least one dispatched message satisfies the predicate function. If no messages meet the condition
        /// or no messages are dispatched, it returns false.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
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

            use cts =
                new CancellationTokenSource(TimeSpan.FromMilliseconds(Config.TimeoutLengthMilliseconds))

            let task =
                task {
                    cmd |> List.iter exec
                    return! tcs.Task
                }

            task.Wait(cts.Token)
            task.Result

        /// <summary>
        /// Determines whether all messages dispatched by the provided command satisfy the given predicate function.
        /// </summary>
        /// <remarks>
        /// This function requires that each command invokes its dispatch function. If a command fails to do so within
        /// the configured timeout period, the function will timeout and throw an OperationCanceledException.
        /// The timeout duration can be adjusted with <see cref="Config.TimeoutLengthMilliseconds"/>.
        /// </remarks>
        /// <param name="predicate">
        /// A function that tests whether a given message fulfills a specific condition.
        /// </param>
        /// <param name="cmd">The command (Cmd) that dispatches messages to be tested by the predicate function.</param>
        /// <returns>
        /// Returns false if a least one message does not meed the condition. Otherwise returns true.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
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

            use cts =
                new CancellationTokenSource(TimeSpan.FromMilliseconds(Config.TimeoutLengthMilliseconds))

            let task =
                task {
                    cmd |> List.iter exec
                    return! tcs.Task
                }

            task.Wait(cts.Token)
            task.Result

        /// <summary>
        /// Captures all messages dispatched by the provided command.
        /// </summary>
        /// <remarks>
        /// This function requires that each command invokes its dispatch function. If a command fails to do so within
        /// the configured timeout period, the function will timeout and throw an OperationCanceledException.
        /// The timeout duration can be adjusted with <see cref="Config.TimeoutLengthMilliseconds"/>.
        /// The order of the captured messages in the result list corresponds to the order of dispatches in the command.
        /// </remarks>
        /// <param name="cmd">The command (Cmd) that dispatches messages to be captured.</param>
        /// <returns>
        /// A list of all messages dispatched by the command. If the command is Cmd.none, an empty list is returned.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
        /// </exception>
        let captureMessages (cmd: Cmd<'msg>) =
            let tcs = TaskCompletionSource<'msg list>()
            let mutable pending = cmd.Length
            let syncRoot = obj ()
            let mutable msgs = []

            if pending = 0 then
                tcs.TrySetResult [] |> ignore
            
            let exec subCmd =
                let dispatch msg =
                    lock syncRoot (fun () ->
                        msgs <- msg :: msgs
                        pending <- pending - 1

                        if pending = 0 then
                            tcs.TrySetResult(msgs) |> ignore)

                subCmd dispatch

            use cts =
                new CancellationTokenSource(TimeSpan.FromMilliseconds(Config.TimeoutLengthMilliseconds))

            let task =
                task {
                    cmd |> List.iter exec
                    return! tcs.Task
                }

            task.Wait(cts.Token)
            task.Result
