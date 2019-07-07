open BenchmarkDotNet.Analysers;
open BenchmarkDotNet.Attributes;
open BenchmarkDotNet.Columns;
open BenchmarkDotNet.Configs;
open BenchmarkDotNet.Exporters;
open BenchmarkDotNet.Exporters.Csv;
open BenchmarkDotNet.Jobs;
open BenchmarkDotNet.Loggers;

type BenchmarkConfig() =
    // Configure your benchmarks, see for more details: https://benchmarkdotnet.org/articles/configs/configs.html.
     ManualConfig
            .Create(DefaultConfig.Instance)
            .With(Job.ShortRun.With(Runtime.Mono))
            .With(Job.ShortRun.With(Runtime.Core))
            .With(MemoryDiagnoser.Default)
            .With(MarkdownExporter.GitHub)
            .With(ExecutionValidator.FailOnError)
    inherit ManualConfig()
    new() = BenchmarkConfig()
