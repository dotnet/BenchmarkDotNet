// This file is NOT meant to be compiled as part of our Integration Tests
// It is just here for reference, see PretendFSharpTest.CS for more information
module BenchmarkSpec

open BenchmarkDotNet.Attributes
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
    let reports = BenchmarkRunner().Run<Db>()
    ()


// Can't get NUnit test runner to work in VS, so "simulate" it by calling the Test method from the EntryPoint method (main())
[<EntryPoint>]
let main argv = 
    printfn "Running NUnit Test Method: ShouldExecuteBenchmark()"
    ShouldExecuteBenchmark()
    0 // return an integer exit code