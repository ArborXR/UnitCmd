namespace Elmish.Test.NUnitExt

open Elmish
open NUnit.Framework
open Elmish.Test.Core

module Assert =
    module Cmd =
        /// <summary>
        /// Asserts that at least one message dispatched by the provided command satisfies the given predicate function.
        /// </summary>
        /// <remarks>
        /// This function uses <see cref="Cmd.Dispatches.exists"/> to check if any message satisfies the predicate.
        /// If not, it asserts with a failure message.
        /// </remarks>
        /// <param name="predicate">
        /// A function that tests whether a given message fulfills a specific condition.
        /// </param>
        /// <param name="cmd">The command (Cmd) that dispatches messages to be tested by the predicate function.</param>
        /// <exception cref="AssertionException">
        /// Thrown if no dispatched message satisfies the predicate function.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
        /// </exception>
        let exists (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
            Assert.True(cmd |> Cmd.Dispatches.exists predicate, "No dispatched message satisfied the predicate.")

        /// <summary>
        /// Asserts that all messages dispatched by the provided command satisfy the given predicate function.
        /// </summary>
        /// <remarks>
        /// This function uses <see cref="Cmd.Dispatches.forall"/> to check if all messages satisfy the predicate.
        /// If not, it asserts with a failure message.
        /// </remarks>
        /// <param name="predicate">
        /// A function that tests whether a given message fulfills a specific condition.
        /// </param>
        /// <param name="cmd">The command (Cmd) that dispatches messages to be tested by the predicate function.</param>
        /// <exception cref="AssertionException">
        /// Thrown if one or more dispatched messages do not satisfy the predicate function.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation exceeds the configured timeout period, or one or more commands do not invoke their
        /// dispatch function.
        /// </exception>
        let forall (predicate: 'msg -> bool) (cmd: Cmd<'msg>) =
            Assert.True(cmd |> Cmd.Dispatches.forall predicate, "One or more dispatched messages did not satisfy the predicate.")

