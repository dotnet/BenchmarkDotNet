module FSharpBenchmark

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open System.Threading

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
