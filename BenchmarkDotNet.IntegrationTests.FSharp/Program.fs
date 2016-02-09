module FSharpBenchmark

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open NUnit.Framework
open FsUnit
open System.Threading

type File = 
    { Name : string
      Path : string
      Extension : string
      Length : int }

[<Config("jobs=dry")>]
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

[<Test>]
let ShouldExecuteBenchmark() = 
    let reports = BenchmarkRunner.Run<Db>()
    ()


// Can't get NUnit test runner to work in VS, so "simulate" it by calling the Test method from the EntryPoint method (main())
[<EntryPoint>]
let main argv = 
    printfn "Running Test Method: ShouldExecuteBenchmark()"
    ShouldExecuteBenchmark()
    0 // return an integer exit code
