module _BenchmarkProjectName_ 

open System
open BenchmarkDotNet
open BenchmarkDotNet.Attributes

#if config
[<Config(typedefof<Configs.BenchmarkConfig>)>]
#endif
type $(BenchmarkName) () =
    [<Params(0, 1, 15, 100)>]
    member val public sleepTime = 0 with get, set

    // [<GlobalSetup>]
    // member self.GlobalSetup() =
    //     printfn "%s" "Global Setup"

    // [<GlobalCleanup>]
    // member self.GlobalCleanup() =
    //     printfn "%s" "Global Cleanup"

    // [<IterationSetup>]
    // member self.IterationSetup() =
    //     printfn "%s" "Iteration Setup"
    
    // [<IterationCleanup>]
    // member self.IterationCleanup() =
    //     printfn "%s" "Iteration Cleanup"

    [<Benchmark>]
    member this.Thread () = System.Threading.Thread.Sleep(this.sleepTime)

    [<Benchmark>]
    member this.Task () = System.Threading.Tasks.Task.Delay(this.sleepTime)

    [<Benchmark>]
    member this.AsyncToTask () = Async.Sleep(this.sleepTime) |> Async.StartAsTask

    [<Benchmark>]
    member this.AsyncToSync () = Async.Sleep(this.sleepTime) |> Async.RunSynchronously


