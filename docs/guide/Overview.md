# Overview

## Install

Create new console application and install the [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/) NuGet package. We support:

* Project: Classic (`*.csproj`), Modern (`*.xproj`/`project.json`)
* Runtimes: Full .NET Framework, .NET Core, Mono
* OS: Windows, Linux, MacOS
* Languages: C#, F#, VB

## Create benchmark

Write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we 
compare [MD5](https://en.wikipedia.org/wiki/MD5) and [SHA256](https://en.wikipedia.org/wiki/SHA-2) cryptographic hash functions:

```cs
public class Md5VsSha256
{
    private readonly byte[] data = new byte[10000];
    private readonly SHA256 sha256 = SHA256.Create();
    private readonly MD5 md5 = MD5.Create();

    public Md5VsSha256()
    {
        new Random(42).NextBytes(data);
    }

    [Benchmark]
    public byte[] Sha256() => sha256.ComputeHash(data);

    [Benchmark]
    public byte[] Md5() => md5.ComputeHash(data);
}
```

## Run benchmark

It's very simple, just call `BenchmarkRunner.Run`:

```cs
var summary = BenchmarkRunner.Run<Md5VsSha256>();
```

Notice, that you should use only the `Release` configuration for your benchmarks. Otherwise, the results will not correspond to reality. If you forgot to change the configuration, BenchmarkDotNet will print a warning.

## Benchmark results

```
Host Process Environment Information:
BenchmarkDotNet-Dev.Core=v0.9.8.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4702MQ CPU 2.20GHz, ProcessorCount=8
Frequency=2143477 ticks, Resolution=466.5317 ns, Timer=TSC
CLR=MS.NET 4.0.30319.42000, Arch=32-bit RELEASE
GC=Concurrent Workstation
JitModules=clrjit-v4.6.1586.0

Type=Md5VsSha256  Mode=Throughput  GarbageCollection=Concurrent Workstation

 Method |      Median |    StdDev |
------- |------------ |---------- |
 Sha256 | 129.9503 us | 2.0627 us |
    Md5 |  26.1774 us | 0.6362 us |
```

## Jobs

You can check several environments at once. For example, you can compare performance of Full .NET Framework, .NET Core, and Mono. Just add the `ClrJob`, `MonoJob`, `CoreJob` attributes before the class declaration (it requires a `project.json` based project, installed CoreCLR and Mono):

```cs
[ClrJob, MonoJob, CoreJob]
public class Md5VsSha256
```

The result:

```
 Method | Toolchain | Runtime |      Median |    StdDev |
------- |---------- |-------- |------------ |---------- |
    Md5 |       Clr |     Clr |  25.6889 us | 1.1810 us |
 Sha256 |       Clr |     Clr | 129.6621 us | 3.2028 us |
    Md5 |      Core |    Core |  24.0038 us | 0.1683 us |
 Sha256 |      Core |    Core |  57.5698 us | 0.5385 us |
    Md5 |      Mono |    Mono |  53.8173 us | 0.4953 us |
 Sha256 |      Mono |    Mono | 205.7487 us | 2.8628 us |
```

There are a lot of predefined jobs which you can use. For example, you can compare `LegacyJitX86` vs `LegacyJitX64` vs `RyuJITx64`:

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
            Add(Job.
                Default.
                With(Platform.X86).
                With(Jit.LegacyJit).
                With(Runtime.Clr).
                WithLaunchCount(3).
                WithWarmupCount(5).
                WithTargetCount(10));
        }
    }
```

See also:  [Jobs](Configuration/Jobs.htm)


## Columns

You can also add custom columns to the summary table:

```cs
[MinColumn, MaxColumn]
public class Md5VsSha256
```

Result:

```cs
 Method |      Median |    StdDev |         Min |         Max |
------- |------------ |---------- |------------ |------------ |
 Sha256 | 131.3200 us | 4.6744 us | 129.8216 us | 147.7630 us |
    Md5 |  26.2847 us | 0.4424 us |  25.8442 us |  27.4258 us |
```

Of course, you can define own columns based on full benchmark summary.

## Exporters

You can export result of your benchmark in different formats:

```cs
[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter]
public class Md5VsSha256
```

If you have installed R, you can even generate a lot of nice plots:

```cs
[RPlotExporter]
public class Md5VsSha256
```

An image example:

![Overview-RPlot.png](Images/Overview-RPlot.png)

## Languages

You can also write you benchmarks on F# or VB. Examples:

```fs
type StringKeyComparison () =
    let mutable arr : string [] = [||]
    let dict1 = ConcurrentDictionary<_,_>()
    let dict2 = ConcurrentDictionary<_,_>(StringComparer.Ordinal)

    [<Params (100, 500, 1000, 2000)>] 
    member val public DictSize = 0 with get, set

    [<Setup>]
    member self.SetupData() =
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

## BenchmarkRunner

There are several ways to run your benchmarks:

**Types**

```cs
var summary = BenchmarkRunner.Run<MyBenchmarkClass>();
var summary = BenchmarkRunner.Run(typeof(MyBenchmarkClass));
```

**Url**

You can also run a benchmark directly from the internet:

```cs
string url = "<E.g. direct link to raw content of a gist>";
var summary = BenchmarkRunner.RunUrl(url);
```

**Source**

```cs
string benchmarkSource = "public class MyBenchmarkClass { ...";
var summary = BenchmarkRunner.RunSource(benchmarkSource);
```

**BenchmarkSwitcher**

Or you can create a set of benchmarks and choose one from command line:

```cs
static void Main(string[] args)
{
    var switcher = new BenchmarkSwitcher(new[] {
        typeof(BenchmarkClass1),
        typeof(BenchmarkClass2),
        typeof(BenchmarkClass3)
    });
    switcher.Run(args);
}
```

Also you can use the config command style to specify some config via switcher or even command line:

```cs
switcher.Run(new[] { "jobs=dry", "columns=min,max" });
```

## Params

**WIP**

## Diagnostics

**WIP**

## Analyzers and Validators

**WIP**

## Baseline

**WIP**