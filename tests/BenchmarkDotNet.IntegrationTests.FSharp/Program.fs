module FSharpBenchmarks

open System.Threading
open System.Collections.Generic
open BenchmarkDotNet.Attributes

type File = 
    { Name : string
      Path : string
      Extension : string
      Length : int }

[<DryJob>]
type Db() = 

    let createDoc name = 
        { Name = name
          Path = name
          Extension = name
          Length = name.Length }

    [<Benchmark>]
    member this.Test() = 
        printfn "// ### F# Benchmark method called ###"
        Thread.Sleep(50)
        createDoc("Testing")

type TestEnum = | A = 0 | B = 1 | C = 2

type EnumParamsTest() =
    let mutable collectedParams = HashSet<TestEnum>()

    [<Params(TestEnum.A, TestEnum.B)>]
    member val EnumParamValue = TestEnum.A with get, set

    [<Benchmark>]
    member this.Benchmark() =
        if not <| collectedParams.Contains(this.EnumParamValue) then
            printfn "// ### New Parameter %A ###" this.EnumParamValue
            collectedParams.Add(this.EnumParamValue) |> ignore