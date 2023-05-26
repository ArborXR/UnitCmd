namespace Elmish.Test.Core

open System
open System.Threading
open Elmish
open System.Threading.Tasks

module Cmd =
    /// <summary>
    /// Starts the provided commands with a no-op dispatch function.
    /// </summary>
    /// <param name="cmd">Command to be executed.</param>
    /// <remarks>
    /// This function initiates the command but does not wait for its completion.
    /// If you need to wait for a command's completion that dispatches messages,
    /// refer to the <see cref="Cmd.Await"/> module.
    /// Conversely, if the command does not dispatch messages and there's a need to wait for its completion,
    /// use the <see cref="Cmd.Delay.start"/> function.
    /// </remarks>
    let start (cmd: Cmd<'msg>) =
        let dispatch: Dispatch<'msg> = fun _ -> ()
        cmd |> List.iter (fun call -> call dispatch)

    /// <summary>
    /// Determines if any message dispatched by the provided command satisfies the given predicate function.
    /// </summary>
    /// <remarks>
    /// Note that this function does not wait for the command to complete. If you need to wait for a command's
    /// dispatch to complete, refer to the <see cref="Cmd.Await"/> and <see cref="Cmd.Delay"/> modules.
    /// </remarks>
    /// <param name="predicate">
    /// A function that tests whether a given message fulfills a specific condition.
    /// </param>
    /// <param name="cmd">The command (Cmd) that dispatches messages to be tested by the predicate function.</param>
    /// <returns>
    /// True if at least one dispatched message satisfies the predicate function. If no messages meet the condition
    /// or no messages are dispatched, it returns false.
    /// </returns>
    let exists (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
        let mutable predicateIsSatisfied = false

        let execute subCmd =
            let dispatch msg =
                if predicate msg then
                    predicateIsSatisfied <- true
                
            subCmd dispatch
            
        cmd |> List.iter execute
        predicateIsSatisfied

    /// <summary>
    /// Determines whether all messages dispatched by the provided command satisfy the given predicate function.
    /// </summary>
    /// <remarks>
    /// Note that this function does not wait for the command to complete. If you need to wait for a command's
    /// dispatch to complete, refer to the <see cref="Cmd.Await"/> and <see cref="Cmd.Delay"/> modules.
    /// </remarks>
    /// <param name="predicate">
    /// A function that tests whether a given message fulfills a specific condition.
    /// </param>
    /// <param name="cmd">The command (Cmd) that dispatches messages to be tested by the predicate function.</param>
    /// <returns>
    /// Returns false if a least one message does not meed the condition. Otherwise returns true.
    /// </returns>
    let forall (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
        let mutable predicateIsSatisfied = true

        let execute subCmd =
            let dispatch msg =
                if msg |> predicate |> not then
                    predicateIsSatisfied <- false
                
            subCmd dispatch
            
        cmd |> List.iter execute
        predicateIsSatisfied
    
    /// <summary>
    /// Captures all messages dispatched by the provided command.
    /// </summary>
    /// <remarks>
    /// Note that this function does not wait for the command to complete. If you need to wait for a command's
    /// dispatch to complete, refer to the <see cref="Cmd.Await"/> and <see cref="Cmd.Delay"/> modules.
    /// </remarks>
    /// <param name="cmd">The command (Cmd) that dispatches messages to be captured.</param>
    /// <returns>
    /// A list of all messages dispatched by the command.
    /// </returns>
    let captureMessages (cmd: Cmd<'msg>) =
        let mutable msgs = []

        let dispatch: Dispatch<'msg> =
            fun msg ->
                msgs <- msg :: msgs

        cmd |> List.iter (fun call -> call dispatch)
        msgs |> List.rev
  
    module Await =
        let private awaitDispatch (defaultValue: 'res) (onMsg: 'msg -> 'res -> 'res) (cmd: Cmd<'msg>) =
            let tcs = TaskCompletionSource<'res>()
            let mutable pending = cmd.Length
            let mutable res = defaultValue
            let syncRoot = obj ()

            if pending = 0 then
                tcs.TrySetResult defaultValue |> ignore

            let exec subCmd =
                let dispatch msg =
                    lock syncRoot (fun () ->
                        res <- onMsg msg res
                        pending <- pending - 1

                        if pending = 0 then
                            tcs.TrySetResult(res) |> ignore)

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
        /// Runs the provided commands, and waits for all commands to invoke their dispatch function.
        /// </summary>
        /// <remarks>
        /// This function requires that each command invokes its dispatch function. If a command fails to do so within
        /// the configured timeout period, the function will timeout and throw an OperationCanceledException.
        /// The timeout duration can be adjusted with <see cref="Config.TimeoutLengthMilliseconds"/>.
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
        /// </exception>
        let run (cmd: Cmd<'msg>) =
            cmd |> awaitDispatch () (fun _ _ -> ())
        
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
            let checkMsg msg res = if predicate msg then true else res
            cmd |> awaitDispatch false checkMsg

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
            let checkMsg msg res = if predicate msg then res else false
            cmd |> awaitDispatch true checkMsg

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
        /// A list of all messages dispatched by the command.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
        /// </exception>
        let captureMessages (cmd: Cmd<'msg>) =
            let addMsg msg res = msg :: res
            cmd |> awaitDispatch [] addMsg

    module Delay =
        /// <summary>
        /// Starts the provided command then waits for specified delay.
        /// </summary>
        /// <param name="delay">The delay duration after the command is initiated.</param>
        /// <param name="cmd">Command to be executed.</param>
        /// <remarks>
        /// This function initiates the command, then pauses the execution for the specified delay duration.
        /// This is useful when there's a need to wait for the completion of an asynchronous command, but the
        /// command does not invoke its dispatch function. If the command does invoke its dispatch function,
        /// refer to the <see cref="Cmd.Await"/> module.
        /// </remarks>
        let start (delay: TimeSpan) (cmd: Cmd<'msg>) =
            start cmd
            delay |> Async.Sleep |> Async.RunSynchronously

        /// <summary>
        /// Determines if any message dispatched by the provided command satisfies the given predicate function.
        /// </summary>
        /// <remarks>
        /// This function initiates the command, then pauses the execution for the specified delay duration.
        /// This is useful when there's a need to wait for the completion of an asynchronous command, but the
        /// command does not invoke its dispatch function. If the command does invoke its dispatch function,
        /// refer to the <see cref="Cmd.Await"/> module.
        /// </remarks>
        /// <param name="delay">The delay duration after the command is initiated.</param>
        /// <param name="predicate">
        /// A function that tests whether a given message fulfills a specific condition.
        /// </param>
        /// <param name="cmd">
        /// The command (Cmd) that dispatches messages to be tested by the predicate function.
        /// </param>
        let exists (delay: TimeSpan) (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
            let mutable predicateIsSatisfied = false
            let execute subCmd =
                let dispatch msg =
                    if predicate msg then
                        predicateIsSatisfied <- true
                    
                subCmd dispatch
                
            cmd |> List.iter execute
            delay |> Async.Sleep |> Async.RunSynchronously
            predicateIsSatisfied
    
        /// <summary>
        /// Determines if all messages dispatched by the provided command satisfy the given predicate function.
        /// </summary>
        /// <remarks>
        /// This function initiates the command, then pauses the execution for the specified delay duration.
        /// This is useful when there's a need to wait for the completion of an asynchronous command, but the
        /// command does not invoke its dispatch function. If the command does invoke its dispatch function,
        /// refer to the <see cref="Cmd.Await"/> module.
        /// </remarks>
        /// <param name="delay">The delay duration after the command is initiated.</param>
        /// <param name="predicate">
        /// A function that tests whether a given message fulfills a specific condition.
        /// </param>
        /// <param name="cmd">
        /// The command (Cmd) that dispatches messages to be tested by the predicate function.
        /// </param>
        let forall (delay: TimeSpan) (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
            let mutable predicateIsSatisfied = true
            let execute subCmd =
                let dispatch msg =
                    if msg |> predicate |> not then
                        predicateIsSatisfied <- false
                    
                subCmd dispatch
                
            cmd |> List.iter execute
            delay |> Async.Sleep |> Async.RunSynchronously
            predicateIsSatisfied
    
        /// <summary>
        /// Captures all messages dispatched by the provided command.
        /// </summary>
        /// <remarks>
        /// This function initiates the command, then pauses the execution for the specified delay duration.
        /// This is useful when there's a need to wait for the completion of an asynchronous command, but the
        /// command does not invoke its dispatch function. If the command does invoke its dispatch function,
        /// refer to the <see cref="Cmd.Await"/> module.
        /// </remarks>
        /// <param name="delay">The delay duration after the command is initiated.</param>
        /// <param name="cmd">
        /// The command (Cmd) that dispatches messages to be tested by the predicate function.
        /// </param>
        let captureMessages (delay: TimeSpan) (cmd: Cmd<'msg>) =
            let mutable msgs = []

            let dispatch: Dispatch<'msg> =
                fun msg ->
                    msgs <- msg :: msgs

            cmd |> List.iter (fun call -> call dispatch)
            delay |> Async.Sleep |> Async.RunSynchronously
            msgs |> List.rev
    