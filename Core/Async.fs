namespace Elmish.Test.Core

open System.Threading

module private Async =
    let withCancellation (token: CancellationToken) (computation: Async<'a>) =
        async {
            token.ThrowIfCancellationRequested()
            return! computation
        }

    let withTimeout (timeout: int) (computation: Async<'a>) =
        async {
            let! child = Async.StartChild(computation, timeout)
            return! child
        }

