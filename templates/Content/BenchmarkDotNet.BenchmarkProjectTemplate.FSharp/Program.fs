open System
open BenchmarkDotNet.Running
open _BenchmarkProjectName_

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<$(BenchmarkName)>() |> ignore
    0 // return an integer exit code