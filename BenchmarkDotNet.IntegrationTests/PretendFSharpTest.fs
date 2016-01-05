// This file is NOT meant to be compiled as part of our Integration Tests
// It is just here for reference, see PretendFSharpTest.CS for more information
module BenchmarkSpec

open BenchmarkDotNet
open NUnit.Framework
open FsUnit
open BenchmarkDotNet.Tasks
open System.Threading

type File = 
    { Name : string
      Path : string
      Extension : string
      Length : int }

[<BenchmarkTask(platform = BenchmarkPlatform.X86, jitVersion = BenchmarkJitVersion.LegacyJit, 
                mode = BenchmarkMode.SingleRun, processCount = 1, warmupIterationCount = 1, targetIterationCount = 1)>]
[<BenchmarkTask(platform = BenchmarkPlatform.X64, jitVersion = BenchmarkJitVersion.LegacyJit,
                mode = BenchmarkMode.SingleRun, processCount = 1, warmupIterationCount = 1, targetIterationCount = 1)>]
type Db() = 

    let createDoc name = 
        { Name = name
          Path = name
          Extension = name
          Length = name.Length }

    [<Benchmark>]
    member this.Test() = 
        printfn "// ### F# Benchmark method called ###"
        Thread.Sleep(500)
        createDoc("Testing")

[<Test>]
let ShouldExecuteBenchmark() = 
    let reports = BenchmarkRunner().Run<Db>()
    ()


// Can't get NUnit test runner to work in VS, so "simulate" it by calling the Test method from the EntryPoint method (main())
[<EntryPoint>]
let main argv = 
    printfn "Running NUnit Test Method: ShouldExecuteBenchmark()"
    ShouldExecuteBenchmark()
    0 // return an integer exit code