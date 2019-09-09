module Configs 

open BenchmarkDotNet.Configs
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Validators

type BenchmarkConfig() as self =

    // Configure your benchmarks, see for more details: https://benchmarkdotnet.org/articles/configs/configs.html.
    inherit ManualConfig() 
    do
        self
            .With(MemoryDiagnoser.Default)
            .With(MarkdownExporter.GitHub)
            .With(ExecutionValidator.FailOnError)
            |> ignore
