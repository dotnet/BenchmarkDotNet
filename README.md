
![](docs/logo/logo-wide.png)


[![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) [![Gitter](https://img.shields.io/gitter/room/dotnet/BenchmarkDotNet.svg)](https://gitter.im/dotnet/BenchmarkDotNet)  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md) [![Overview](https://img.shields.io/badge/docs-Overview-green.svg?style=flat)](https://benchmarkdotnet.org/articles/overview.html) [![ChangeLog](https://img.shields.io/badge/docs-ChangeLog-green.svg?style=flat)](https://benchmarkdotnet.org/changelog/index.html)

| Build server | Platform | Build status |
|--------------|----------|--------------|
| Azure Pipelines | Windows | [![Azure Pipelines Windows](https://dev.azure.com/dotnet/BenchmarkDotNet/_apis/build/status/BenchmarkDotNet%20-%20Windows)](https://dev.azure.com/dotnet/BenchmarkDotNet/_build/latest?definitionId=55) |
| Azure Pipelines | Ubuntu  | [![Azure Pipelines Ubuntu](https://dev.azure.com/dotnet/BenchmarkDotNet/_apis/build/status/BenchmarkDotNet%20-%20Ubuntu)](https://dev.azure.com/dotnet/BenchmarkDotNet/_build/latest?definitionId=56) |
| Azure Pipelines | macOS | [![Azure Pipelines macOS](https://dev.azure.com/dotnet/BenchmarkDotNet/_apis/build/status/BenchmarkDotNet%20-%20macOS)](https://dev.azure.com/dotnet/BenchmarkDotNet/_build/latest?definitionId=57) |
| AppVeyor | Windows | [![AppVeyor/Windows](https://img.shields.io/appveyor/ci/dotnetfoundation/benchmarkdotnet/master.svg)](https://ci.appveyor.com/project/dotnetfoundation/benchmarkdotnet/branch/master) |
| Travis | Linux | [![Travis/Linux](https://travis-matrix-badges.herokuapp.com/repos/dotnet/BenchmarkDotNet/branches/master/1)](https://travis-ci.org/dotnet/BenchmarkDotNet) |     
| Travis | macOS | [![Travis/macOS](https://travis-matrix-badges.herokuapp.com/repos/dotnet/BenchmarkDotNet/branches/master/2)](https://travis-ci.org/dotnet/BenchmarkDotNet) |

**BenchmarkDotNet** is a powerful .NET library for benchmarking.

Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
BenchmarkDotNet will protect you from the common pitfalls (even for experienced developers) because it does all the dirty work for you:
  it generates an isolated project per each benchmark method,
  does several launches of this project,
  run multiple iterations of the method (include warm-up), and so on.
Usually, you even shouldn't care about a number of iterations because BenchmarkDotNet chooses it automatically to achieve the requested level of precision.

It's really easy to design a performance experiment with BenchmarkDotNet.
Just mark your method with the [Benchmark] attribute and the benchmark is ready.
Want to run your code on .NET Framework, .NET Core, CoreRT, and Mono?
No problem: a few more attributes and the corresponded projects will be generated; the results will be presented at the same summary table.
In fact, you can compare any environment that you want:
  you can check performance difference between processor architectures (x86/x64),
  JIT versions (LegacyJIT/RyuJIT),
  different sets of GC flags (like Server/Workstation),
  and so on.
You can also introduce one or several parameters and check the performance on different inputs at once.

BenchmarkDotNet helps you not only run benchmarks but also analyze the results: it generates reports in different formats and renders nice plots.
It calculates many statistics, allows you to run statistical tests, and compares results of different benchmark methods.
So it doesn't overload you with data, by default BenchmarkDotNet prints only the really important statistical values depending on your results:
  it allows you to keep summary small and simple for primitive cases but notify you about an additional important area for complicated cases
  (of course, you can request any numbers manually via additional attributes).

BenchmarkDotNet doesn't just blindly run your code: it tries to help you to conduct a qualitative performance investigation.

## Showtime

It's very easy to start using BenchmarkDotNet.
Let's look at an example:

```cs
using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace MyBenchmarks
{
    [ClrJob(baseline: true), CoreJob, MonoJob, CoreRtJob]
    [RPlotExporter, RankColumn]
    public class Md5VsSha256
    {
        private SHA256 sha256 = SHA256.Create();
        private MD5 md5 = MD5.Create();
        private byte[] data;

        [Params(1000, 10000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Md5VsSha256>();
        }
    }
}
```

BenchmarkDotNet allows designing a performance experiment in a user-friendly declarative way.
At the end of an experiment, it will generate a summary table which contains only important data in a compact and understandable form:

```
BenchmarkDotNet=v0.11.0, OS=Windows 10.0.16299.309 (1709/FallCreatorsUpdate/Redstone3)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
Frequency=3507504 Hz, Resolution=285.1030 ns, Timer=TSC
.NET Core SDK=2.1.300-preview1-008174
  [Host]     : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-HKEEXO : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2633.0
  Core       : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  CoreRT     : .NET CoreRT 1.0.26414.01, 64bit AOT
  Mono       : Mono 5.10.0 (Visual Studio), 64bit 

| Method | Runtime |     N |       Mean |     Error |    StdDev | Ratio | Rank |
|------- |-------- |------ |-----------:|----------:|----------:|------:|-----:|
| Sha256 |     Clr |  1000 |   8.009 us | 0.0370 us | 0.0346 us |  1.00 |    3 |
| Sha256 |    Core |  1000 |   4.447 us | 0.0117 us | 0.0110 us |  0.56 |    2 |
| Sha256 |  CoreRT |  1000 |   4.321 us | 0.0139 us | 0.0130 us |  0.54 |    1 |
| Sha256 |    Mono |  1000 |  14.924 us | 0.0574 us | 0.0479 us |  1.86 |    4 |
|        |         |       |            |           |           |       |      |
|    Md5 |     Clr |  1000 |   3.051 us | 0.0604 us | 0.0742 us |  1.00 |    3 |
|    Md5 |    Core |  1000 |   2.004 us | 0.0058 us | 0.0054 us |  0.66 |    2 |
|    Md5 |  CoreRT |  1000 |   1.892 us | 0.0087 us | 0.0077 us |  0.62 |    1 |
|    Md5 |    Mono |  1000 |   3.878 us | 0.0181 us | 0.0170 us |  1.27 |    4 |
|        |         |       |            |           |           |       |      |
| Sha256 |     Clr | 10000 |  75.780 us | 1.0445 us | 0.9771 us |  1.00 |    3 |
| Sha256 |    Core | 10000 |  41.134 us | 0.2185 us | 0.1937 us |  0.54 |    2 |
| Sha256 |  CoreRT | 10000 |  40.895 us | 0.0804 us | 0.0628 us |  0.54 |    1 |
| Sha256 |    Mono | 10000 | 141.377 us | 0.5598 us | 0.5236 us |  1.87 |    4 |
|        |         |       |            |           |           |       |      |
|    Md5 |     Clr | 10000 |  18.575 us | 0.0727 us | 0.0644 us |  1.00 |    3 |
|    Md5 |    Core | 10000 |  17.562 us | 0.0436 us | 0.0408 us |  0.95 |    2 |
|    Md5 |  CoreRT | 10000 |  17.447 us | 0.0293 us | 0.0244 us |  0.94 |    1 |
|    Md5 |    Mono | 10000 |  34.500 us | 0.1553 us | 0.1452 us |  1.86 |    4 |
```

In artifacts, you can also find detailed information about each iteration.
You can export the data in different formats like (CSV, XML, JSON, and so on) or even generate beautiful plots:

![rplot.png](docs/images/rplot.png)

## Main features

BenchmarkDotNet has a lot of awesome features for deep performance investigations:

* **Standard benchmarking routine:** generating an isolated project per each benchmark method; auto-selection of iteration amount; warmup; overhead evaluation; and so on
* **Execution control:** BenchmarkDotNet tries to choose the best possible way to evaluate performance, but you can also manually control amount of iterations, switch between cold start and warmed state, set the accuracy level, tune GC parameters, change environment variables, and more
* **Statistics:** by default, you will see the most important statistics like mean and standard deviation; but you can also manually ask for min/max values, confidence intervals, skewness, kurtosis, quartile, percentiles, or define own metrics
* **Comparing environments:** [Easy way](https://benchmarkdotnet.org/articles/configs/jobs.html) to compare different environments (x86 vs x64, LegacyJit vs RyuJit, Mono vs .NET Core, and so on)
* **Relative performance:** you can [easily](https://benchmarkdotnet.org/articles/features/baselines.html) evaluate difference between different methods of environments
* **Memory diagnostics:** the library not only measure performance of your code, but also prints information about memory traffic and amount of GC collections
* **Disassembly diagnostics:** you can ask for an assembly listing with the help of single additional attribute
* **Parametrization:** performance can be evaluated for different sets of input [parameters](https://benchmarkdotnet.org/articles/features/parameterization.html) like in popular unit test frameworks
* **Environment information:** when your share performance results, it's very important to share information about your environment; BenchmarkDotNet automatically prints the exact version of your OS and processor; amount of physical CPU, physical cores, and logic cores; hypervisor (if you use it); frequency of the hardware timer; the JIT-compiler version; and more
* **Command-line support:** you can manage thousands of benchmark, group them by categories, [filter](https://benchmarkdotnet.org/articles/configs/filters.html) and run them from [command line](https://benchmarkdotnet.org/articles/guides/console-args.html)
* **Powerful reporting system:** it's possible to export benchmark results to markdown, csv, html, plain text, png plots

A few useful links for you:

* If you want to know more about BenchmarkDotNet features, check out the [Overview Page](https://benchmarkdotnet.org/articles/overview.html).
* If you want to use BenchmarkDotNet for the first time, the [Getting Started](https://benchmarkdotnet.org/articles/guides/getting-started.html) will help you.
* If you want to ask a quick question or discuss performance topics, use the [gitter](https://gitter.im/dotnet/BenchmarkDotNet) channel.

## Supported technologies

BenchmarkDotNet supports all kinds of .NET stacks:

* **Supported runtimes:** .NET Framework (4.6.1+), .NET Core (2.0+), Mono, CoreRT
* **Supported languages:** C#, F#, Visual Basic
* **Supported OS:** Windows, Linux, macOS

## Our users

The library is used by a large number of projects for performance discussions or as a part of the codebase:

* [dotnet/performance](https://github.com/dotnet/performance) (official benchmarks used for testing the performance of all .NET Runtimes)
* [CoreCLR](https://github.com/dotnet/coreclr/issues?utf8=✓&q=BenchmarkDotNet) (.NET Core Runtime)
* [CoreFX](https://github.com/dotnet/corefx/issues?utf8=✓&q=BenchmarkDotNet) (.NET Core Base Class Libraries)
* [Roslyn](https://github.com/dotnet/roslyn/search?q=BenchmarkDotNet&type=Issues&utf8=✓) (C# and Visual Basic compiler)
* [KestrelHttpServer](https://github.com/aspnet/KestrelHttpServer/tree/master/benchmarks/Kestrel.Performance) (A cross platform web server for ASP.NET Core)
* [SignalR](https://github.com/aspnet/SignalR/tree/master/benchmarks/Microsoft.AspNetCore.SignalR.Microbenchmarks)
* [EntityFrameworkCore](https://github.com/aspnet/EntityFrameworkCore/tree/master/benchmark)
* [F#](https://github.com/fsharp/fsharp/blob/master/tests/scripts/array-perf/array-perf.fs)
* [Orleans](https://github.com/dotnet/orleans/tree/master/test/Benchmarks)
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/tree/master/Src/Newtonsoft.Json.Tests/Benchmarks)
* [Elasticsearch.Net](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/bool-queries.html#_perfomance_considerations)
* [Dapper](https://github.com/StackExchange/Dapper/tree/master/Dapper.Tests.Performance)
* [Expecto](https://github.com/haf/expecto/tree/master/Expecto.BenchmarkDotNet)
* [Accord.NET Framework](https://github.com/accord-net/framework/tree/development/Tools/Performance)
* [ImageSharp](https://github.com/SixLabors/ImageSharp/tree/master/tests/ImageSharp.Benchmarks)
* [RavenDB](https://github.com/ravendb/ravendb/tree/v4.0/bench)
* [NodaTime](https://github.com/nodatime/nodatime/tree/master/src/NodaTime.Benchmarks)
* [Jint](https://github.com/sebastienros/jint/tree/dev/Jint.Benchmark)
* [NServiceBus](https://github.com/Particular/NServiceBus/issues?utf8=✓&q=+BenchmarkDotNet+)
* [Serilog](https://github.com/serilog/serilog/tree/dev/test/Serilog.PerformanceTests)
* [Autofac](https://github.com/autofac/Autofac/tree/develop/bench/Autofac.Benchmarks)
* [Npgsql](https://github.com/npgsql/npgsql/tree/dev/test/Npgsql.Benchmarks)

It's not the full list.
On GitHub, you can find hundreds of
  [issues](https://github.com/search?o=desc&q=BenchmarkDotNet+-repo:dotnet%2FBenchmarkDotNet&s=created&type=Issues&utf8=✓) and
  [commits](https://github.com/search?o=desc&q=BenchmarkDotNet+-repo:dotnet%2FBenchmarkDotNet&s=committer-date&type=Commits&utf8=✓)
  which involve BenchmarkDotNet.
There are [tens of thousands of files](https://github.com/search?o=desc&q=BenchmarkDotNet+-repo:dotnet%2FBenchmarkDotNet&s=indexed&type=Code&utf8=✓)
  which contain "BenchmarkDotNet".

## Contributions are welcome!

BenchmarkDotNet is already a stable full-featured library which allows performing performance investigation on a professional level.
And it continues to evolve!
We add new features all the time, but we have too many new cool ideas.
Any help will be appreciated.
You can develop new features, fix bugs, improve the documentation, or do some other cool stuff.

If you want to contribute, check out the
  [Contributing guide](https://benchmarkdotnet.org/articles/contributing/building.html) and
  [up-for-grabs](https://github.com/dotnet/BenchmarkDotNet/issues?q=is:open+is:issue+label:up-for-grabs) issues.
If you have new ideas or want to complain about bugs, feel free to [create a new issue](https://github.com/dotnet/BenchmarkDotNet/issues/new).
Let's build the best tool for benchmarking together!

## Code of Conduct

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
