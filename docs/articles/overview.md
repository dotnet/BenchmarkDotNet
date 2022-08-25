---
uid: docs.overview
name: Overview
---

# Overview

## Install

Create new console application and install the [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/) NuGet package. We support:

* *Projects:* classic and modern with PackageReferences
* *Runtimes:* Full .NET Framework (4.6+), .NET Core (2.0+), Mono, NativeAOT
* *OS:* Windows, Linux, MacOS
* *Languages:* C#, F#, VB

## Design a benchmark
Create a new console application, write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we 
compare the [MD5](https://en.wikipedia.org/wiki/MD5) and [SHA256](https://en.wikipedia.org/wiki/SHA-2) cryptographic hash functions:

```cs
using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace MyBenchmarks
{
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
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
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
```

The `BenchmarkRunner.Run<Md5VsSha256>()` call runs your benchmarks and print results to console output.

Notice, that you should use only the `Release` configuration for your benchmarks.
Otherwise, the results will not correspond to reality.
If you forgot to change the configuration, BenchmarkDotNet will print a warning.

## Benchmark results

```
BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.472 (1803/April2018Update/Redstone4)
Intel Core i7-2630QM CPU 2.00GHz (Sandy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=1948699 Hz, Resolution=513.1629 ns, Timer=TSC
.NET Core SDK=2.1.502
  [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT


| Method |      Mean |     Error |    StdDev |
|------- |----------:|----------:|----------:|
| Sha256 | 100.90 us | 0.5070 us | 0.4494 us |
|    Md5 |  37.66 us | 0.1290 us | 0.1207 us |
```

## Jobs

You can check several environments at once. For example, you can compare performance of Full .NET Framework, .NET Core, Mono and NativeAOT. Just add the `ClrJob`, `MonoJob`, `CoreJob`, attributes before the class declaration (it requires .NET SDK and Mono to be installed and added to $PATH):

```cs
[ClrJob, MonoJob, CoreJob]
public class Md5VsSha256
```

Example of the result:

```ini
BenchmarkDotNet=v0.11.0, OS=Windows 10.0.16299.309 (1709/FallCreatorsUpdate/Redstone3)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
Frequency=3507504 Hz, Resolution=285.1030 ns, Timer=TSC
.NET Core SDK=2.1.300-preview1-008174
  [Host]     : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-YRHGTP : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2633.0
  Core       : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  CoreRT     : .NET CoreRT 1.0.26414.01, 64bit AOT
  Mono       : Mono 5.10.0 (Visual Studio), 64bit 

| Method | Runtime |       Mean |     Error |    StdDev |
|------- |-------- |-----------:|----------:|----------:|
| Sha256 |     Clr |  75.780 us | 1.0445 us | 0.9771 us |
| Sha256 |    Core |  41.134 us | 0.2185 us | 0.1937 us |
| Sha256 |  CoreRT |  40.895 us | 0.0804 us | 0.0628 us |
| Sha256 |    Mono | 141.377 us | 0.5598 us | 0.5236 us |
|        |         |            |           |           |
|    Md5 |     Clr |  18.575 us | 0.0727 us | 0.0644 us |
|    Md5 |    Core |  17.562 us | 0.0436 us | 0.0408 us |
|    Md5 |  CoreRT |  17.447 us | 0.0293 us | 0.0244 us |
|    Md5 |    Mono |  34.500 us | 0.1553 us | 0.1452 us |
```

There are a lot of predefined jobs which you can use. For example, you can compare `LegacyJitX86` vs `LegacyJitX64` vs `RyuJitX64`:

```cs
[LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
```

Or you can define own jobs:

```cs
[Config(typeof(Config))]
public class Md5VsSha256
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(new Job(EnvMode.LegacyJitX86, EnvMode.Clr, RunMode.Dry)
                {
                    Env = { Runtime = Runtime.Clr },
                    Run = { LaunchCount = 3, WarmupCount = 5, TargetCount = 10 },
                    Accuracy = { MaxStdErrRelative = 0.01 }
                }));
        }
    }
```

Read more:  [Jobs](configs/jobs.md), [Configs](configs/configs.md)


## Columns

You can also add custom columns to the summary table:

```cs
[MinColumn, MaxColumn]
public class Md5VsSha256
```

| Method | Median      | StdDev    | Min         | Max         |
| ------ | ----------- | --------- | ----------- | ----------- |
| Sha256 | 131.3200 us | 4.6744 us | 129.8216 us | 147.7630 us |
| Md5    | 26.2847 us  | 0.4424 us | 25.8442 us  | 27.4258 us  |

Of course, you can define own columns based on full benchmark summary.

Read more:  [Columns](configs/columns.md)

## Exporters

You can export result of your benchmark in different formats:

```cs
[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
public class Md5VsSha256
```

If you have installed R, `RPlotExporter` will generate a lot of nice plots:

![](../images/v0.12.0/rplot.png)

Read more:  [Exporters](configs/exporters.md)

## Baseline

In order to scale your results you need to mark one of your benchmark methods as a `Baseline`:

```cs
public class Sleeps
{
    [Benchmark]
    public void Time50() => Thread.Sleep(50);

    [Benchmark(Baseline = true)]
    public void Time100() => Thread.Sleep(100);

    [Benchmark]
    public void Time150() => Thread.Sleep(150);
}
```

As a result, you will have additional column in the summary table:

| Method  | Median      | StdDev    | Ratio |
| ------- | ----------- | --------- | ------ |
| Time100 | 100.2640 ms | 0.1238 ms | 1.00   |
| Time150 | 150.2093 ms | 0.1034 ms | 1.50   |
| Time50  | 50.2509 ms  | 0.1153 ms | 0.50   |

Read more:  [Baselines](features/baselines.md)

## Params

You can mark one or several fields or properties in your class by the `Params` attribute. In this attribute, you can specify set of values. As a result, you will get results for each combination of params values.

```cs
public class IntroParams
{
    [Params(100, 200)]
    public int A { get; set; }

    [Params(10, 20)]
    public int B { get; set; }

    [Benchmark]
    public void Benchmark()
    {
        Thread.Sleep(A + B + 5);
    }
}
```

| Method    | Median      | StdDev    | A    | B    |
| --------- | ----------- | --------- | ---- | ---- |
| Benchmark | 115.3325 ms | 0.0242 ms | 100  | 10   |
| Benchmark | 125.3282 ms | 0.0245 ms | 100  | 20   |
| Benchmark | 215.3024 ms | 0.0375 ms | 200  | 10   |
| Benchmark | 225.2710 ms | 0.0434 ms | 200  | 20   |

Read more:  [Parameterization](features/parameterization.md)

## Languages

You can also write you benchmarks on `F#` or `VB`. Examples:

```fs
type StringKeyComparison () =
    let mutable arr : string [] = [||]
    let dict1 = ConcurrentDictionary<_,_>()
    let dict2 = ConcurrentDictionary<_,_>(StringComparer.Ordinal)

    [<Params (100, 500, 1000, 2000)>] 
    member val public DictSize = 0 with get, set

    [<GlobalSetup>]
    member self.GlobalSetupData() =
        dict1.Clear(); dict2.Clear()
        arr <- getStrings self.DictSize
        arr |> Array.iter (fun x -> dict1.[x] <- true ; dict2.[x] <- true)

    [<Benchmark>]
    member self.StandardLookup () = lookup arr dict1

    [<Benchmark>]
    member self.OrdinalLookup () = lookup arr dict2
```

```vb
Public Class Sample
    <Params(1, 2)>
    Public Property A As Integer
    <Params(3, 4)>
    Public Property B As Integer

    <Benchmark>
    Public Function Benchmark() As Integer
            return A + B
    End Function
End Class
```

## Diagnostics

A **diagnoser** can attach to your benchmark and get some useful info.

The current Diagnosers are:

- GC and Memory Allocation (`MemoryDiagnoser`) which is cross platform, built-in and **is not enabled by default anymore**.
- JIT Inlining Events (`InliningDiagnoser`). You can find this diagnoser in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`): [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/)


Below is a sample output from the `MemoryDiagnoser`, note the extra columns on the right-hand side (`Gen 0` and `Allocated`):

```
    Method |       Mean |    StdDev |  Gen 0 | Allocated |
---------- |----------- |---------- |------- |---------- |
 Iterative | 31.0739 ns | 0.1091 ns |      - |       0 B |
      LINQ | 83.0435 ns | 1.0103 ns | 0.0069 |      32 B | 
```

Read more:  [Diagnosers](configs/diagnosers.md)

## BenchmarkRunner

There are several ways to run your benchmarks: you can use an existing class, run a benchmark based on code from internet or based on source code:

```cs
var summary = BenchmarkRunner.Run<MyBenchmarkClass>();
var summary = BenchmarkRunner.Run(typeof(MyBenchmarkClass));

string url = "<E.g. direct link to a gist>";
var summary = BenchmarkRunner.RunUrl(url);

string benchmarkSource = "public class MyBenchmarkClass { ...";
var summary = BenchmarkRunner.RunSource(benchmarkSource);
```

Read more:  [HowToRun](guides/how-to-run.md)
