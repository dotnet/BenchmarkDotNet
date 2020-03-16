![](logo/logo-wide.png)

[![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) [![Gitter](https://img.shields.io/gitter/room/dotnet/BenchmarkDotNet.svg)](https://gitter.im/dotnet/BenchmarkDotNet)  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](articles/license.md) [![Overview](https://img.shields.io/badge/docs-Overview-green.svg?style=flat)](xref:docs.overview) [![ChangeLog](https://img.shields.io/badge/docs-ChangeLog-green.svg?style=flat)](xref:changelog)

**BenchmarkDotNet** is a powerful .NET library for benchmarking.

Source code is available at [github.com/dotnet/BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)

Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
BenchmarkDotNet will protect you from the common pitfalls (even for experienced developers) because it does all the dirty work for you:
  it generates an isolated project per each benchmark method,
  does several launches of this project,
  run multiple iterations of the method (include warm-up), and so on.
Usually, you even shouldn't care about a number of iterations because BenchmarkDotNet chooses it automatically to achieve the requested level of precision.

It's really easy to design a performance experiment with BenchmarkDotNet.
Just mark your method with the [Benchmark] attribute and the benchmark is ready.
Want to run your code on .NET Framework, .NET Core, and Mono?
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
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    [SimpleJob(RuntimeMoniker.NetCoreApp30)]
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    [SimpleJob(RuntimeMoniker.Mono)]
    [RPlotExporter]
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

```text
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.805 (1809/October2018Update/Redstone5)
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
  [Host]       : .NET Framework 4.7.2 (4.7.3468.0), X64 RyuJIT
  Net472       : .NET Framework 4.7.2 (4.7.3468.0), X64 RyuJIT
  NetCoreApp30 : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  CoreRt30     : .NET CoreRT 1.0.28231.02 @Commit: 741d61493c560ba96e8151f9e56876d4d3828489, X64 AOT
  Mono         : Mono 6.4.0 (Visual Studio), X64


| Method |       Runtime |     N |       Mean |     Error |    StdDev | Ratio |
|------- |-------------- |------ |-----------:|----------:|----------:|------:|
| Sha256 |    .NET 4.7.2 |  1000 |   7.735 us | 0.1913 us | 0.4034 us |  1.00 |
| Sha256 | .NET Core 3.0 |  1000 |   3.989 us | 0.0796 us | 0.0745 us |  0.50 |
| Sha256 |    CoreRt 3.0 |  1000 |   4.091 us | 0.0811 us | 0.1562 us |  0.53 |
| Sha256 |          Mono |  1000 |  13.117 us | 0.2485 us | 0.5019 us |  1.70 |
|        |               |       |            |           |           |       |
|    Md5 |    .NET 4.7.2 |  1000 |   2.872 us | 0.0552 us | 0.0737 us |  1.00 |
|    Md5 | .NET Core 3.0 |  1000 |   1.848 us | 0.0348 us | 0.0326 us |  0.64 |
|    Md5 |    CoreRt 3.0 |  1000 |   1.817 us | 0.0359 us | 0.0427 us |  0.63 |
|    Md5 |          Mono |  1000 |   3.574 us | 0.0678 us | 0.0753 us |  1.24 |
|        |               |       |            |           |           |       |
| Sha256 |    .NET 4.7.2 | 10000 |  74.509 us | 1.5787 us | 4.6052 us |  1.00 |
| Sha256 | .NET Core 3.0 | 10000 |  36.049 us | 0.7151 us | 1.0025 us |  0.49 |
| Sha256 |    CoreRt 3.0 | 10000 |  36.253 us | 0.7076 us | 0.7571 us |  0.49 |
| Sha256 |          Mono | 10000 | 116.350 us | 2.2555 us | 3.0110 us |  1.58 |
|        |               |       |            |           |           |       |
|    Md5 |    .NET 4.7.2 | 10000 |  17.308 us | 0.3361 us | 0.4250 us |  1.00 |
|    Md5 | .NET Core 3.0 | 10000 |  15.726 us | 0.2064 us | 0.1930 us |  0.90 |
|    Md5 |    CoreRt 3.0 | 10000 |  15.627 us | 0.2631 us | 0.2461 us |  0.89 |
|    Md5 |          Mono | 10000 |  30.205 us | 0.5868 us | 0.6522 us |  1.74 |
```

In artifacts, you can also find detailed information about each iteration.
You can export the data in different formats like (CSV, XML, JSON, and so on) or even generate beautiful plots:

![](images/v0.12.0/rplot.png)

## Main features

BenchmarkDotNet has a lot of awesome features for deep performance investigations:

* **Standard benchmarking routine:** generating an isolated project per each benchmark method; auto-selection of iteration amount; warmup; overhead evaluation; and so on
* **Execution control:** BenchmarkDotNet tries to choose the best possible way to evaluate performance, but you can also manually control amount of iterations, switch between cold start and warmed state, set the accuracy level, tune GC parameters, change environment variables, and more
* **Statistics:** by default, you will see the most important statistics like mean and standard deviation; but you can also manually ask for min/max values, confidence intervals, skewness, kurtosis, quartile, percentiles, or define own metrics
* **Comparing environments:** [Easy way](http://benchmarkdotnet.org/Configs/Jobs.htm) to compare different environments (x86 vs x64, LegacyJit vs RyuJit, Mono vs .NET Core, and so on)
* **Relative performance:** you can [easily](http://benchmarkdotnet.org/Advanced/Baseline.htm) evaluate difference between different methods of environments
* **Memory diagnostics:** the library not only measure performance of your code, but also prints information about memory traffic and amount of GC collections
* **Disassembly diagnostics:** you can ask for an assembly listing with the help of single additional attribute
* **Parametrization:** performance can be evaluated for different sets of input [parameters](http://benchmarkdotnet.org/Advanced/Params.htm) like in popular unit test frameworks
* **Environment information:** when your share performance results, it's very important to share information about your environment; BenchmarkDotNet automatically prints the exact version of your OS and processor; amount of physical CPU, physical cores, and logic cores; hypervisor (if you use it); frequency of the hardware timer; the JIT-compiler version; and more
* **Command-line support:** you can manage thousands of benchmark, group them by categories, [filter](http://benchmarkdotnet.org/Configs/Filters.htm) and run them from command line
* **Powerful reporting system:** it's possible to export benchmark results to markdown, csv, html, plain text, png plots

A few useful links for you:

* If you want to know more about BenchmarkDotNet features, check out the [Overview Page](http://benchmarkdotnet.org/Overview.htm).
* If you want to use BenchmarkDotNet for the first time, the [Getting Started](http://benchmarkdotnet.org/GettingStarted.htm) will help you.
* If you want to ask a quick question or discuss performance topics, use the [gitter](https://gitter.im/dotnet/BenchmarkDotNet) channel.

## Supported technologies

BenchmarkDotNet supports all kinds of .NET stacks:

* **Supported runtimes:** .NET Framework (4.6+), .NET Core (2.0+), Mono, CoreRT
* **Supported languages:** C#, F#, Visual Basic
* **Supported OS:** Windows, Linux, macOS

## Our users

The library is used by a large number of projects for performance discussions or as a part of the codebase:

* [CoreCLR](https://github.com/dotnet/coreclr/issues?utf8=✓&q=BenchmarkDotNet) (.NET Core runtime)
* [CoreFX](https://github.com/dotnet/corefx/issues?utf8=✓&q=BenchmarkDotNet) (.NET Core foundational libraries;
  see also [official benchmarking guide](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-corefx.md)),
* [Roslyn](https://github.com/dotnet/roslyn/search?q=BenchmarkDotNet&type=Issues&utf8=✓) (C# and Visual Basic compiler)
* [KestrelHttpServer](https://github.com/aspnet/KestrelHttpServer/tree/dev/benchmarks/Kestrel.Performance) (A cross platform web server for ASP.NET Core)
* [SignalR](https://github.com/aspnet/SignalR/tree/dev/benchmarks/Microsoft.AspNetCore.SignalR.Microbenchmarks)
* [EntityFrameworkCore](https://github.com/aspnet/EntityFrameworkCore/tree/dev/benchmarks)
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
  [Contributing guide](http://benchmarkdotnet.org/Contributing.htm) and
  [up-for-grabs](https://github.com/dotnet/BenchmarkDotNet/issues?q=is:open+is:issue+label:up-for-grabs) issues.
If you have new ideas or want to complain about bugs, feel free to [create a new issue](https://github.com/dotnet/BenchmarkDotNet/issues/new).
Let's build the best tool for benchmarking together!

## Code of Conduct

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
