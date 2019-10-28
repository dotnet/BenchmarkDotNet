<h3 align="center">

  ![](docs/logo/logo-wide.png)

</h3>

<h3 align="center">

  [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/)
  [![Downloads](https://img.shields.io/nuget/dt/benchmarkdotnet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/)
  [![Gitter](https://img.shields.io/gitter/room/dotnet/BenchmarkDotNet.svg)](https://gitter.im/dotnet/BenchmarkDotNet)
  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

</h3>

<h3 align="center">
  <a href="https://benchmarkdotnet.org/articles/guides/getting-started.html">Getting started</a>
  <span> · </span>
  <a href="https://benchmarkdotnet.org/articles/overview.html">Overview</a>
  <span> · </span>
  <a href="https://benchmarkdotnet.org/changelog/index.html">ChangeLog</a>
</h3>

**BenchmarkDotNet** is a powerful .NET library for benchmarking.
It helps you not only run benchmarks but also analyze the results: it generates reports in different formats and renders beautiful plots.
It calculates many statistics, allows you to run statistical tests, and compares results of different benchmark methods.
So it doesn't overload you with data, by default BenchmarkDotNet prints only the essential statistical values depending on your results:
  it allows you to keep summary small and simple for primitive cases but notify you about an additional important area for complicated cases
  (of course, you can request any numbers manually via additional attributes).

It's really easy to design a performance experiment with BenchmarkDotNet.
Just mark your method with the `[Benchmark]` attribute and the benchmark is ready.
Want to run your code on .NET Framework, .NET Core, CoreRT, and Mono?
No problem: a few more attributes and the corresponded projects will be generated; the results will be presented at the same summary table.
In fact, you can compare any environment that you want:
  you can check performance difference between processor architectures (x86/x64),
  JIT versions (LegacyJIT/RyuJIT),
  different sets of GC flags (like Server/Workstation),
  and so on.
You can also introduce one or several parameters and check the performance on different inputs at once.

BenchmarkDotNet doesn't just blindly run your code: it tries to help you to conduct a qualitative performance investigation.

* **Why do I need a special library for benchmarking?**  
  Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
  BenchmarkDotNet will protect you from the common pitfalls (even if are an experienced developer) because it does all the dirty work for you:
    it generates an isolated project per each benchmark method,
    does several launches of this project,
    run multiple iterations of the method (include warm-up), and so on.
  Usually, you even shouldn't care about a number of iterations because BenchmarkDotNet chooses it automatically to achieve the requested level of precision.
* **Who use BenchmarkDotNet?**  
  Everyone!
  BenchmarkDotNet is already adopted by more than [3000+](https://github.com/dotnet/BenchmarkDotNet/network/dependents?package_id=UGFja2FnZS0xNTY3MzExMzE%3D) projects including
  [dotnet/performance](https://github.com/dotnet/performance) (official benchmarks used for testing the performance of all .NET Runtimes),
  [CoreCLR](https://github.com/dotnet/coreclr/issues?utf8=✓&q=BenchmarkDotNet) (.NET Core Runtime),
  [CoreFX](https://github.com/dotnet/corefx/issues?utf8=✓&q=BenchmarkDotNet) (.NET Core Base Class Libraries),
  [Roslyn](https://github.com/dotnet/roslyn/search?q=BenchmarkDotNet&type=Issues&utf8=✓) (C# and Visual Basic compiler),
  [KestrelHttpServer](https://github.com/aspnet/KestrelHttpServer/tree/master/benchmarks/Kestrel.Performance) (A cross platform web server for ASP.NET Core),
  [SignalR](https://github.com/aspnet/SignalR/tree/master/benchmarks/Microsoft.AspNetCore.SignalR.Microbenchmarks),
  [EntityFrameworkCore](https://github.com/aspnet/EntityFrameworkCore/tree/master/benchmark),
  [F#](https://github.com/fsharp/fsharp/blob/master/tests/scripts/array-perf/array-perf.fs),
  [Orleans](https://github.com/dotnet/orleans/tree/master/test/Benchmarks),
  [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/tree/master/Src/Newtonsoft.Json.Tests/Benchmarks),
  [Elasticsearch.Net](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/bool-queries.html#_perfomance_considerations),
  [Dapper](https://github.com/StackExchange/Dapper/tree/master/Dapper.Tests.Performance),
  [Expecto](https://github.com/haf/expecto/tree/master/Expecto.BenchmarkDotNet),
  [Accord.NET Framework](https://github.com/accord-net/framework/tree/development/Tools/Performance),
  [ImageSharp](https://github.com/SixLabors/ImageSharp/tree/master/tests/ImageSharp.Benchmarks),
  [RavenDB](https://github.com/ravendb/ravendb/tree/v4.0/bench),
  [NodaTime](https://github.com/nodatime/nodatime/tree/master/src/NodaTime.Benchmarks),
  [Jint](https://github.com/sebastienros/jint/tree/dev/Jint.Benchmark),
  [NServiceBus](https://github.com/Particular/NServiceBus/issues?utf8=✓&q=+BenchmarkDotNet+),
  [Serilog](https://github.com/serilog/serilog/tree/dev/test/Serilog.PerformanceTests),
  [Autofac](https://github.com/autofac/Autofac/tree/develop/bench/Autofac.Benchmarks),
  [Npgsql](https://github.com/npgsql/npgsql/tree/dev/test/Npgsql.Benchmarks),
  [Avalonia](https://github.com/AvaloniaUI/Avalonia/tree/master/tests/Avalonia.Benchmarks),
  [dotnet/machinelearning](https://github.com/dotnet/machinelearning/tree/master/test/Microsoft.ML.Benchmarks),
  [ASP.NET Core](https://github.com/aspnet/AspNetCore/tree/master/src/Servers/IIS/IIS/benchmarks),
  [ReactiveUI](https://github.com/reactiveui/ReactiveUI/tree/master/src/Benchmarks).  
  Of course, it's not the full list:
    on GitHub, you can find thousands of
    [issues](https://github.com/search?o=desc&q=BenchmarkDotNet+-repo:dotnet%2FBenchmarkDotNet&s=created&type=Issues&utf8=✓) and
    [commits](https://github.com/search?o=desc&q=BenchmarkDotNet+-repo:dotnet%2FBenchmarkDotNet&s=committer-date&type=Commits&utf8=✓)
    that involve BenchmarkDotNet.
  There are [hundreds of thousands of files](https://github.com/search?o=desc&q=BenchmarkDotNet+-repo:dotnet%2FBenchmarkDotNet&s=indexed&type=Code&utf8=✓)
    which contain "BenchmarkDotNet".
* **What technologies are supported?**  
  *Supported runtimes:* .NET Framework (4.6.1+), .NET Core (2.0+), Mono, CoreRT  
  *Supported languages:* C#, F#, Visual Basic  
  *Supported OS:* Windows, Linux, macOS  
* **Where I can find more information about benchmarking methodology?**  
  You can find a lot of useful information in this book: ["Pro .NET Benchmarking"](https://aakinshin.net/prodotnetbenchmarking/).
  Use this in-depth guide to correctly design benchmarks, measure key performance metrics of .NET applications, and analyze results.
  This book presents dozens of case studies to help you understand complicated benchmarking topics.
  You will avoid common pitfalls, control the accuracy of your measurements, and improve the performance of your software.


## Content

- [Showtime](#showtime)
- [Features](#features)
  - [Simple Automation](#simple-automation)
  - [Rich API](#rich-api)
  - [Detailed Reports](#detailed-reports)
  - [Powerful Diagnostics](#powerful-diagnostics)
- [Pro .NET Benchmarking](#pro-net-benchmarking)
- [Build status](#build-status)
- [Contributions are welcome!](#contributions-are-welcome)
- [Code of Conduct](#code-of-conduct)
- [.NET Foundation](#net-foundation)

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

## Features

BenchmarkDotNet has a lot of excellent features for deep performance investigations!

### Simple Automation

* **Boilerplate generation**  
  By default, BenchmarkDotNet generates an isolated project per each benchmark method,
    detects the best number of method invocations,
    performs the warmup iterations,
    performs the actual iterations,
    helps to you protect benchmarks from different JIT-optimizations (like dead code elimination or loop unrolling),
    evaluate the call overhead,
    and implement other benchmarking routines.
  You can learn about the under-the-hood activities [here](https://benchmarkdotnet.org/articles/guides/how-it-works.html).
* **Optimal precision level**  
  BenchmarkDotNet tries to choose the best benchmarking parameters and
    achieve a good trade-off between the measurement prevision and the total duration of all benchmark runs.
  So, you shouldn't use any magic numbers (like "We should perform 100 iterations here"),
    BenchmarkDotNet will do it for you based on the values of statistical metrics.
  Of course, you can turn off all the magic and use explicit numbers if you want.
* **Benchmark isolation**  
  By default, each benchmark will be executed in its own separate process that guarantees proper benchmark isolation:
    if one benchmark spoils the runtime environment, other benchmarks will not be affected.
  Such infrastructure also allows easily to benchmark the same set of benchmarks on different runtimes.
* **Result aggregation**  
  BenchmarkDotNet will also collect all the measurements from different processes for you and aggregate them into a single report.

### Rich API

* **Tons of useful APIs**  
  BenchmarkDotNet has thousands of different [APIs](https://benchmarkdotnet.org/api/index.html) that allows customizing everything!
* **Different API styles**  
  BenchmarkDotNet has several API styles, so you can choose the style that is most suitable for your use cases.
  For example, if you want to execute a benchmark using .NET Core 3.0, you can express you with in different ways:  
  *Attributes:* `[SimpleJob(RuntimeMoniker.NetCoreApp30)]`  
  *Fluent API:* `BenchmarkRunner.Run<MyBench>(ManualConfig.CreateEmpty().With(Job.Default.With(CoreRuntime.Core30)))`  
  *Command line:* `dotnet benchmark MyAssembly.dll --filter MyBench --runtime netcoreapp3.0`
* **Parametrization**  
  With the help of [parameterization](https://benchmarkdotnet.org/articles/features/parameterization.html),
    you can run the same benchmark against different values of a specified parameter.
* **Comparing environments**  
  With the help of [jobs](https://benchmarkdotnet.org/articles/configs/jobs.html) you can easily compare different environments (like x86 vs. x64, LegacyJit vs. RyuJit, Mono vs .NET Core, and so on).

### Detailed Reports

* **Summary table**  
  BenchmarkDotNet will generate a summary table that contains a lot of useful data about the executed benchmarks.
  By default, it contains only the most important columns, but the column set can be [easily customized](https://benchmarkdotnet.org/articles/configs/columns.html).
  The default column set is adaptive and depends on the benchmark definition and measured values.
  For example, if you mark one of the benchmarks as a [baseline](https://benchmarkdotnet.org/articles/features/baselines.html),
   you will get additional columns that will help you to compare all the benchmarks with the baseline.
* **Environment information**  
  When your share performance results, it's essential to share information about your environment.
  To help you with that, BenchmarkDotNet automatically collects and prints
    the exact version of your OS and processor;
    amount of physical CPU, physical cores, and logic cores;
    hypervisor title (if you use it);
    frequency of the hardware timer;
    the JIT-compiler version;
    and other useful information about your current environment.
* **Statistics metrics**  
  BenchmarkDotNet has a powerful statistics engine that can
    calculate different metrics (from mean and standard deviation to confidence intervals and skewness),
    perform statistical tests (like [Mann–Whitney U test](https://en.wikipedia.org/wiki/Mann%E2%80%93Whitney_U_test)),
    build plain text histograms,
    detect changepoints (e.g., using [ED-PELT](https://aakinshin.net/posts/edpelt/) algorithm).
* **Export**  
  We have several groups of [export formats](https://benchmarkdotnet.org/articles/configs/exporters.html):  
 *Human-readable summary:*
   markdown (including GitHub, StackOverflow, Atlassian, and other flavors),
   AsciiDoc,
   html  
 *Machine-readable raw data:*
   [csv](https://benchmarkdotnet.org/articles/configs/exporters.html#csv),
   [json](https://benchmarkdotnet.org/articles/configs/exporters.html#sample-introexportjson),
   [xml](https://benchmarkdotnet.org/articles/configs/exporters.html#sample-introexportxml)  
 *Images*:
   [png plots](https://benchmarkdotnet.org/articles/configs/exporters.html#plots)

### Powerful Diagnostics

* **Environment diagnostics**  
  BenchmarkDotNet prevents benchmarking of non-optimized assemblies (e.g., that was built using DEBUG mode) because
    the corresponding results will be unreliable.
  Also, it will print a warning you if you have an attached debugger, if you use hypervisor (like HyperV, VMware, or VirtualBox),
    or if you have any other problems with the current environment.
* **Measurement diagnostics**  
  BenchmarkDotNet tries to find some unusual properties of your performance distributions and prints nice messages about it.
  For example, it will warn you in case of multimodal distribution or high outliers.
* **Memory diagnostics**  
  You can annotate your benchmark class with special attributes and get useful information about memory.  
  [[MemoryDiagnoser]](https://benchmarkdotnet.org/articles/configs/diagnosers.html#usage) measures the managed memory traffic and the number of GC collections.  
  [[NativeMemoryProfiler]](https://benchmarkdotnet.org/articles/samples/IntroNativeMemory.html) measures the native memory traffic.
* **Disassembly diagnostics**  
  You can also request for an assembly listing with the help of a single additional attribute.  
  [[DisassemblyDiagnoser]](https://benchmarkdotnet.org/articles/configs/diagnosers.html#sample-introdisassembly) generates the native listings and save them into the BenchmarkDotNet.Artifacts folder.

## Pro .NET Benchmarking

BenchmarkDotNet is not a silver bullet that magically makes all of your benchmarks correct and analyzes the measurements for you.
Even if you use this library, you still should know how to design the benchmark experiments and how to make correct conclusions based on the raw data.
If you want to know more about benchmarking methodology and good practices,
  it's recommended to read a book by Andrey Akinshin (the BenchmarkDotNet project lead):
  ["Pro .NET Benchmarking"](https://aakinshin.net/prodotnetbenchmarking/).

<div align="center">
  <a href="https://aakinshin.net/prodotnetbenchmarking/">
    <img src="https://aakinshin.net/img/misc/prodotnetbenchmarking-cover.png" width="400" />
  </a>
</div>

## Build status

| Build server | Platform | Build status |
|--------------|----------|--------------|
| Azure Pipelines | Windows | [![Azure Pipelines Windows](https://dev.azure.com/dotnet/BenchmarkDotNet/_apis/build/status/BenchmarkDotNet%20-%20Windows)](https://dev.azure.com/dotnet/BenchmarkDotNet/_build/latest?definitionId=55) |
| Azure Pipelines | Ubuntu  | [![Azure Pipelines Ubuntu](https://dev.azure.com/dotnet/BenchmarkDotNet/_apis/build/status/BenchmarkDotNet%20-%20Ubuntu)](https://dev.azure.com/dotnet/BenchmarkDotNet/_build/latest?definitionId=56) |
| Azure Pipelines | macOS | [![Azure Pipelines macOS](https://dev.azure.com/dotnet/BenchmarkDotNet/_apis/build/status/BenchmarkDotNet%20-%20macOS)](https://dev.azure.com/dotnet/BenchmarkDotNet/_build/latest?definitionId=57) |
| AppVeyor | Windows | [![AppVeyor/Windows](https://img.shields.io/appveyor/ci/dotnetfoundation/benchmarkdotnet/master.svg)](https://ci.appveyor.com/project/dotnetfoundation/benchmarkdotnet/branch/master) |
| Travis | Linux | [![Travis/Linux](https://travis-matrix-badges.herokuapp.com/repos/dotnet/BenchmarkDotNet/branches/master/1)](https://travis-ci.org/dotnet/BenchmarkDotNet) |     
| Travis | macOS | [![Travis/macOS](https://travis-matrix-badges.herokuapp.com/repos/dotnet/BenchmarkDotNet/branches/master/2)](https://travis-ci.org/dotnet/BenchmarkDotNet) |

## Contributions are welcome!

BenchmarkDotNet is already a stable full-featured library that allows performing performance investigation on a professional level.
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
For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
