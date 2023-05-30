namespace UnitCmd.Core

open System.Threading

module private Async =
    let withCancellation (token: CancellationToken) (computation: Async<'a>) =
        async {
            token.ThrowIfCancellationRequested()
            return! computation
        }

    let withTimeout (timeoutMilliseconds: int) (computation: Async<'a>) =
        async {
            let! child = Async.StartChild(computation, timeoutMilliseconds)
            return! child
        }

