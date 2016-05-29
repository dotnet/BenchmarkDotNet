**BenchmarkDotNet** is a powerful .NET library for benchmarking.

**Links:** [Wiki](https://github.com/PerfDotNet/BenchmarkDotNet/wiki), [ChangeLog](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/ChangeLog), [Developing](DEVELOPING.md)

[![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://img.shields.io/gitter/room/PerfDotNet/BenchmarkDotNet.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md) [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/)

**Summary**

* Standard benchmarking routine: generating an isolated project per each benchmark method; auto-selection of iteration amount; warmup; overhead evaluation; statistics calculation; and so on.
* Easy way to compare different environments (`x86` vs `x64`, `LegacyJit` vs `RyuJit`, and so on; see: [Jobs](#jobs))
* Reports: markdown (default, github, stackoverflow), csv, html, plain text; png plots.
* Advanced features: [Baseline](#baseline), [Params](#params), [Percentiles](#percentiles)
* Powerful diagnostics based on ETW events (see [BenchmarkDotNet.Diagnostics.Windows](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/))
* Supported runtimes: Full .NET Framework, .NET Core (both RC2 and RC1), Mono, Dnx (dnx451-dnx46)
* Supported languages: C#, F# (also on [.NET Core](https://github.com/PerfDotNet/BenchmarkDotNet/issues/135)) and Visual Basic

## Content

* [Getting started](#getting-started)
* [Configs](#configs)
* [Advanced features](#advanced-features)
* [Rules of benchmarking](#rules-of-benchmarking)
* [How to run?](#how-to-run)
* [How it works?](#how-it-works)
* [FAQ](#faq)
* [Team](#team)

## Getting started

**Step 1.** Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```
PM> Install-Package BenchmarkDotNet
```

If you want to use CoreCLR (`netstandard1.5` or `dnxcore50`), you need prerelease version of the package:

```
PM> Install-Package BenchmarkDotNet -Pre
```

**Step 2.** Write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we compare [MD5](https://en.wikipedia.org/wiki/MD5) and [SHA256](https://en.wikipedia.org/wiki/SHA-2) cryptographic hash functions:

```cs
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
    public byte[] Sha256()
    {
        return sha256.ComputeHash(data);
    }

    [Benchmark]
    public byte[] Md5()
    {
        return md5.ComputeHash(data);
    }
}
```

**Step 3.** Run it:

```cs
var summary = BenchmarkRunner.Run<Md5VsSha256>();
```

**Step 4.** View the results. Here is an example of output from the above benchmark:

```ini
BenchmarkDotNet=v0.9.0.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4810MQ CPU @ 2.80GHz, ProcessorCount=8
Frequency=2728067 ticks, Resolution=366.5599 ns
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]

Type=Md5VsSha256  Mode=Throughput
```

 Method |      Median |    StdDev
------- |------------ |----------
    Md5 |  21.2912 us | 0.4373 us
 Sha256 | 107.4124 us | 1.8339 us


**Step 5.** Analyze it. In your bin directory, you can find a lot of useful files with detailed information. For example:
  * Csv reports with raw data: `Md5VsSha256-report.csv`, `Md5VsSha256-runs.csv`
  * Markdown reports:  `Md5VsSha256-report-default.md`, `Md5VsSha256-report-stackoverflow.md`, `Md5VsSha256-report-github.md`
  * Plain report and log: `Md5VsSha256-report.txt`, `Md5VsSha256.log`
  * Plots (if you have installed R): `Md5VsSha256-barplot.png`, `Md5VsSha256-boxplot.png`, and so on.

## Configs

Config is a set of so called `jobs`, `columns`, `exporters`, `loggers`, `diagnosers`, `analysers`, `validators` that help you to build your benchmark. There are two ways to set your config:

* **Object style**

```cs
[Config(typeof(Config))]
public class MyClassWithBenchmarks
{
	private class Config : ManualConfig
    {
    	public Config()
        {
        	Add(new Job1(), new Job2());
            Add(new Column1(), new Column2());
            Add(new Exporter1(), new Exporter2());
            Add(new Logger1(), new Logger2());
            Add(new Diagnoser1(), new Diagnoser2());
            Add(new Analyser1(), new Analyser2());
        }
    }
    
	[Benchmark]
    public void Benchmark1()
    {
    }
    
    [Benchmark]
    public void Benchmark2()
    {
    }
}
```

* **Command style**

```cs
[Config("jobs=job1,job2 " +
        "columns=column1,column2 " +
        "exporters=exporter1,exporter2 " +
        "loggers=logger1,logger2 " +
        "diagnosers=diagnoser1,diagnoser2 " +
        "analysers=analyser1,analyser2")]
public class MyClassWithBenchmarks
{
	[Benchmark]
    public void Benchmark1()
    {
    }
    
    [Benchmark]
    public void Benchmark2()
    {
    }
}
```

* **Custom configs**

You can also define own config attribute:

```cs
[MyConfigSource(Jit.LegacyJit, Jit.RyuJit)]
public class IntroConfigSource
{
    private class MyConfigSourceAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; private set; }

        public MyConfigSourceAttribute(params Jit[] jits)
        {
            var jobs = jits.Select(jit => Job.Dry.With(Platform.X64).With(jit)).ToArray();
            Config = ManualConfig.CreateEmpty().With(jobs);
        }
    }

    [Benchmark]
    public void Foo()
    {
        Thread.Sleep(10);
    }
}
```

* **Fluent config**

There is no need to create new Config type, you can simply use fluent interface:

```cs
BenchmarkRunner
	.Run<Algo_Md5VsSha256>(
		ManualConfig
			.Create(DefaultConfig.Instance)
			.With(Job.RyuJitX64)
			.With(Job.Core)
			.With(ExecutionValidator.FailOnError));
```

### Jobs

A *job* is an environment for your benchmarks. You can set one or several jobs for your set of benchmarks.

Job characteristics:

* **Toolchain.** A toolchain for generating/building/executing your benchmark. Values: `Classic` (csproj based) *[default]*. Coming soon: `Dnx`.
* **Mode.** Values: `Throughput` *[default]*, `SingleRun`.
* **Platform.** Values: `Host` *[default]*, `AnyCpu`, `X86`, `X64`.
* **Jit.** Values: `Host` *[default]*, `LegacyJit`, `RyuJit`.
* **Framework.** Values: `Host` *[default]*, `V40`, `V45`, `V451`, `V452`, `V46`.
* **Runtime.** Values: `Host` *[default]*, `Clr`, `Mono`, `Core`, `Dnx`.
* **LaunchCount.** Count of separated process launches. Values: `Auto` *[default]* or specific number.
* **WarmupCount.** Count of warmup iterations. Values: `Auto` *[default]* or specific number.
* **TargetCount.** Count of target iterations (that will be used for summary). Values: `Auto` *[default]* or specific number.
* **IterationTime.** Desired time of execution of an iteration (in ms). Values: `Auto` *[default]* or specific number.
* **Affinity.** [ProcessorAffinity](https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx) of process. Values: `Auto` *[default]* or specific mask.

The `Host` value means that value will be inherited from host process settings. The `Auto` values means the BenchmarkDotNet automatically choose the best value.

**Predefined**

```cs
class Job
{
    IJob Default = new Job();
    IJob LegacyX86 = new Job { Platform = Platform.X86, Jit = Jit.LegacyJit };
    IJob LegacyX64 = new Job { Platform = Platform.X64, Jit = Jit.LegacyJit };
    IJob RyuJitX64 = new Job { Platform = Platform.X64, Jit = Jit.RyuJit };
    IJob Dry = new Job { Mode = Mode.SingleRun, ProcessCount = 1, WarmupCount = 1, TargetCount = 1 };
    IJob[] AllJits = { LegacyX86, LegacyX64, RyuJitX64 };
    IJob Clr = new Job { Runtime = Runtime.Clr };
    IJob Mono = new Job { Runtime = Runtime.Mono };
    IJob Dnx = new Job { Runtime = Runtime.Dnx };
    IJob Core = new Job { Runtime = Runtime.Core };
    IJob LongRun = new Job { LaunchCount = 3, WarmupCount = 30, TargetCount = 1000 };
}
```

**Examples**

```cs
// *** Command style ***
[Config("jobs=AllJits")]
[Config("jobs=Dry")]
[Config("jobs=LegacyX64,RyuJitX64")]
```

```cs
// *** Object style ***
class Config : ManualConfig
{
    public Config()
    {
    	Add(Job.AllJits);
    	Add(Job.LegacyX64, Job.RyuJitX64);
        Add(Job.Default.With(Mode.SingleRun).WithProcessCount(1).WithWarmupCount(1).WithTargetCount(1));
        Add(Job.Default.With(Framework.V40).With(Runtime.Mono).With(Platform.X64));
    }
}
```

### Columns

A *column* is a column in the summary table.

**Predefined**

```cs
class StatisticColumn
{
    IColumn Mean;
    IColumn StdError;
    IColumn StdDev;
    IColumn OperationPerSecond;
    IColumn Min;
    IColumn Q1;
    IColumn Median;
    IColumn Q3;
    IColumn Max;
    
    IColumn P0;
    IColumn P25;
    IColumn P50;
    IColumn P80;
    IColumn P85;
    IColumn P90;
    IColumn P95;
    IColumn P100;

    IColumn[] AllStatistics = { Mean, StdError, StdDev, OperationsPerSecond, Min, Q1, Median, Q3, Max };
}
// Specify a "place" of each benchmark. Place 1 means a group of the fastest benchmarks, place 2 means the second group, and so on. There are several styles:
class Place
{
    IColumn ArabicNumber; // `1`, `2`, `3`, ...
    IColumn Stars; // `*`, `**`, `***`, ...
}
class PropertyColumn
{
    IColumn Type;
    IColumn Method;
    IColumn Mode;
    IColumn Platform;
    IColumn Jit;
    IColumn Framework;
    IColumn Toolchain;
    IColumn Runtime;
    IColumn LaunchCount;
    IColumn WarmupCount;
    IColumn TargetCount;
    IColumn Affinity;
}
```

**Default**

* `PropertyColumn.Type`
* `PropertyColumn.Method`
* `PropertyColumn.Mode`
* `PropertyColumn.Platform`
* `PropertyColumn.Jit`
* `PropertyColumn.Framework`
* `PropertyColumn.Toolchain`
* `PropertyColumn.Runtime`
* `PropertyColumn.ProcessCount`
* `PropertyColumn.WarmupCount`
* `PropertyColumn.TargetCount`
* `PropertyColumn.Affinity`
* `StatisticColumn.Median`
* `StatisticColumn.StdDev`
* `BaselineDeltaColumn.Default`

**Examples**

```cs
// *** Command style ***
[Config("columns=Min,Max")]
[Config("columns=AllStatistics")]
```

```cs
// *** Object style ***
[Config(typeof(Config))]
public class IntroTags
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(Job.Dry);
            // You can add custom tags per each method using Columns
            Add(new TagColumn("Foo or Bar", name => name.Substring(0, 3)));
            Add(new TagColumn("Number", name => name.Substring(3)));
        }
    }

    [Benchmark] public void Foo1() { /* ... */ }
    [Benchmark] public void Foo12() { /* ... */ }
    [Benchmark] public void Bar3() { /* ... */ }
    [Benchmark] public void Bar34() { /* ... */ }
}
// Result:
//  Method |     Median |    StdDev | Foo or Bar | Number |
// ------- |----------- |---------- |----------- |------- |
//   Bar34 | 10.3636 ms | 0.0000 ms |        Bar |     34 |
//    Bar3 | 10.4662 ms | 0.0000 ms |        Bar |      3 |
//   Foo12 | 10.1377 ms | 0.0000 ms |        Foo |     12 |
//    Foo1 | 10.2814 ms | 0.0000 ms |        Foo |      1 |
```

### Exporters

An *exporter* allows you to export results of your benchmark in different formats. By default, files with results will be located in .\BenchmarkDotNet.Artifacts\results directory. Default exporters are: csv, html and markdown. Here is list of all available exporters:

```cs
public IEnumerable<IExporter> GetExporters()
{
    yield return MarkdownExporter.Default; // produces <BenchmarkName>-report-default.md
    yield return MarkdownExporter.GitHub; // produces <BenchmarkName>-report-github.md
    yield return MarkdownExporter.StackOverflow; // produces <BenchmarkName>-report-stackoverflow.md
    yield return CsvExporter.Default; // produces <BenchmarkName>-report.csv
    yield return CsvMeasurementsExporter.Default; // produces <BenchmarkName>-measurements.csv
    yield return HtmlExporter.Default; // produces <BenchmarkName>-report.html
    yield return PlainExporter.Default; // produces <BenchmarkName>-report.txt
}
```

* **Plots**

If you have installed [R](https://www.r-project.org/), defined `%R_HOME%` variable and used `RPlotExporter.Default` and `CsvMeasurementsExporter.Default` in your config, you will also get nice plots with help of the `BuildPlots.R` script in your bin directory. Examples:

```
<BenchmarkName>-barplot.png
<BenchmarkName>-boxplot.png
<BenchmarkName>-<MethodName>-density.png
<BenchmarkName>-<MethodName>-facetTimeline.png
<BenchmarkName>-<MethodName>-facetTimelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
```

A config example:

```cs
public class Config : ManualConfig
{
    public Config()
    {
        Add(CsvMeasurementsExporter.Default);
        Add(RPlotExporter.Default);
    }
}
```

### Loggers

A **logger** allows you to log results of your benchmark. By default, you can see log on console and in a file (`<BenchmarkName>.log`).

### Diagnosers

A **diagnoser** can attach to your benchmark and get some useful info. There is a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`):

[![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/)


The current Diagnosers are:
- GC and Memory Allocation (`MemoryDiagnoser`)
- JIT Inlining Events (`InliningDiagnoser`)

Below is a sample output from the `GC and Memory Allocation` diagnoser, note the extra columns on the right-hand side ("Gen 0", "Gen 1", "Gen 2" and "Bytes Allocated/Op"):

    Method |  Lookup |     Median |    StdDev | Scaled |    Gen 0 | Gen 1 | Gen 2 | Bytes Allocated/Op |
---------- |-------- |----------- |---------- |------- |--------- |------ |------ |------------------- |
      LINQ | Testing | 49.1154 ns | 0.5301 ns |   2.48 | 1,526.00 |     - |     - |              25.21 |
 Iterative | Testing | 19.8040 ns | 0.0456 ns |   1.00 |        - |     - |     - |               0.00 |


A config example:

```cs
private class Config : ManualConfig
{
    public Config()
    {
        Add(new MemoryDiagnoser());
        Add(new InliningDiagnoser());
    }
}
```

### Analysers

An **analyser** can analyze summary of your benchmarks and produce some useful warnings. For example, `EnvironmentAnalyser` warns you, if you build your application in the DEBUG mode or run it with an attached debugger.

### Validators

A **validator** can validate your benchmarks before they are executed and produce validation errors. If any of the validation errors is critical, then none of the benchmarks will get executed. Available validators are:

* `BaselineValidator.FailOnError` - it checks if more than 1 Benchmark per class has `Baseline = true` applied. This validator is mandatory.
* `JitOptimizationsValidator.(Dont)FailOnError` - it checks whether any of the referenced assemblies is non-optimized. DontFailOnError version is enabled by default.
* `ExecutionValidator.(Dont)FailOnError` - it checks if it is possible to run your benchmarks by executing each of them once. Optional.

## Advanced Features

### Params

You can mark one or several fields or properties in your class by the `Params` attribute. In this attribute, you can specify set of values. As a result, you will get results for each combination of params values.

**Example**

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

   Method  |      Median |    StdDev |   A |  B
---------- |------------ |---------- |---- |---
 Benchmark | 115.3325 ms | 0.0242 ms | 100 | 10
 Benchmark | 125.3282 ms | 0.0245 ms | 100 | 20
 Benchmark | 215.3024 ms | 0.0375 ms | 200 | 10
 Benchmark | 225.2710 ms | 0.0434 ms | 200 | 20

### Setup

If you have some data that you want to initialize, the `[Setup]` method is the best place for this. It will be invoked only once before each iteration.

```cs
private int[] initialValuesArray;
private List<int> initialValuesList;

[Setup]
public void SetupData()
{
    int MaxCounter = 1000;
    initialValuesArray = Enumerable.Range(0, MaxCounter).ToArray();
    initialValuesList = Enumerable.Range(0, MaxCounter).ToList();
}

[Benchmark]
public int ForLoop()
{
    var counter = 0;
    for (int i = 0; i < initialValuesArray.Length; i++)
        counter += initialValuesArray[i];
    return counter;
}

[Benchmark]
public int ForEachList()
{
    var counter = 0;
    foreach (var i in initialValuesList)
        counter += i;
    return counter;
}

```

### Baseline

In order to scale your results you need to mark one of your benchmark methods as a baseline. Only one method in class can have `Baseline = true` applied.

```cs
public class Sleeps
{
    [Benchmark]
    public void Time50()
    {
        Thread.Sleep(50);
    }

    [Benchmark(Baseline = true)]
    public void Time100()
    {
        Thread.Sleep(100);
    }

    [Benchmark]
    public void Time150()
    {
        Thread.Sleep(150);
    }
}
```

As a result, you will have additional column in the summary table:

```ini
BenchmarkDotNet=v0.9.0.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4810MQ CPU @ 2.80GHz, ProcessorCount=8
Frequency=2728067 ticks, Resolution=366.5599 ns
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]

Type=Sleeps  Mode=Throughput
```

  Method |      Median |    StdDev | Scaled
-------- |------------ |---------- |-------
 Time100 | 100.2640 ms | 0.1238 ms |   1.00
 Time150 | 150.2093 ms | 0.1034 ms |   1.50
  Time50 |  50.2509 ms | 0.1153 ms |   0.50


### Percentiles

The percentile represents a higer boundary for specified percengage of a measurements.
For example, 95th percentile = 500ms means that 95% of all samples are not slower than 500ms.
This metric is not very useful in microbenchmarks, as the values from consequent runs have a very narrow distribution.
Hovewer, real-world scenarios often have so-called long tail distribution (due to IO delays, locks, memory access latency and so on), so the average execution time cannot be trusted.

The percentiles allow to include the tail of distribution into the comparison. Hovewer, it requires some preparations steps.

At first, you should have enough runs to count percentiles from. The `TargetCount` in the config should be set to 10-20 runs at least.  
Second, the count of iterations for each run should not be very high, or the peak timings will be averaged.  
 The `IterationTime = 25` works fine for most cases; for long-running benchmarks the `Mode = Mode.SingleRun` will be the best choice. Hovewer, feel free to experiment with the config values.

Third, if you want to be sure that measurements are repeatable, set the `LanuchCount` to 3 or higher.

And the last, don't forget to include the columns into the config. They are not included by default (as said above, these are not too useful for most of the benchmarks).
There're predefined `StatisticColumn.P0`..`StatisticColumn.P100` for absolute timing percentiles and `BaselineDiffColumn.Scaled50`..`BaselineDiffColumn.Scaled95` for relative percentiles.

**The sample:**

Run the IntroPercentiles sample. 
It contains three benchmark methods.
* First delays for 20 ms constantly.
* The second has random delays for 10..30 ms.
* And the third delays for 10ms 85 times of 100 and delays for 40ms 15 times of 100.

Here's the output from the benchmark (some columns removed for brevity):

         Method |     Median |     StdDev | Scaled |         P0 |        P50 |        P80 |        P85 |        P95 |       P100 | ScaledP50 | ScaledP85 | ScaledP95
--------------- |----------- |----------- |------- |----------- |----------- |----------- |----------- |----------- |----------- |---------- |---------- |----------
 ConstantDelays | 20.3813 ms |  0.2051 ms |   1.00 | 20.0272 ms | 20.3813 ms | 20.4895 ms | 20.4954 ms | 20.5869 ms | 21.1471 ms |      1.00 |      1.00 |      1.00
   RandomDelays | 19.8055 ms |  5.7556 ms |   0.97 | 10.0793 ms | 19.8055 ms | 25.4173 ms | 26.5187 ms | 29.0313 ms | 29.4550 ms |      0.97 |      1.29 |      1.41
     RareDelays | 10.3385 ms | 11.4828 ms |   0.51 | 10.0157 ms | 10.3385 ms | 10.5211 ms | 40.0560 ms | 40.3992 ms | 40.4674 ms |      0.51 |      1.95 |      1.96

Note that the 'Scaled' column kinda lies to you. 
The "almost same" RandomDelays method is actually not so performant and the seems-to-be-fastest RareDelays method is 2 times slower 15 times of 100.

Also, it's very easy to screw the results with incorrect setup. For example, the same code being run with
```cs
                new Job
                {
                    TargetCount = 5,
                    IterationTime = 500
                }
```
completely hides the peak values:

         Method |     Median |    StdDev | Scaled |         P0 |        P50 |        P80 |        P85 |        P95 |       P100 | ScaledP50 | ScaledP85 | ScaledP95
--------------- |----------- |---------- |------- |----------- |----------- |----------- |----------- |----------- |----------- |---------- |---------- |----------
 ConstantDelays | 20.2692 ms | 0.0308 ms |   1.00 | 20.1986 ms | 20.2692 ms | 20.2843 ms | 20.2968 ms | 20.3097 ms | 20.3122 ms |      1.00 |      1.00 |      1.00
   RandomDelays | 18.9965 ms | 0.8601 ms |   0.94 | 18.1339 ms | 18.9965 ms | 19.8126 ms | 19.8278 ms | 20.4485 ms | 20.9466 ms |      0.94 |      0.98 |      1.01
     RareDelays | 14.0912 ms | 2.8619 ms |   0.70 | 10.2606 ms | 14.0912 ms | 15.7653 ms | 17.3862 ms | 18.6728 ms | 18.6940 ms |      0.70 |      0.86 |      0.92


## Rules of benchmarking

* **Use the Release build without an attached debugger**

Never use the Debug build for benchmarking. *Never*. The debug version of the target method can run 10–100 times slower. The release mode means that you should have `<Optimize>true</Optimize>` in your csproj file or use [/optimize](https://msdn.microsoft.com/en-us/library/t0hfscdc.aspx) for `csc`. Also your never should use an attached debugger (e.g. Visual Studio or WinDbg) during the benchmarking. The best way is build our benchmark in the Release mode and run it with `cmd`.

* **Try different environments**

Please, don't extrapolate your results. Or do it very carefully.

I remind you again: the results in different environments may vary significantly. If a `Foo1` method is faster than a `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows, it means that the `Foo1` method is faster than the `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows and nothing else. And you can not say anything about methods performance for CLR 2 or .NET Framework 4.6 or LegacyJIT-x64 or x86 or Linux+Mono until you try it. 

* **Avoid dead code elimination**

You should also use the result of calculation. For example, if you run the following code:

```cs
void Foo()
{
    Math.Exp(1);
}
```

then JIT can eliminate this code because the result of `Math.Exp` is not used. The better way is use it like this:

```cs
double Foo()
{
    return Math.Exp(1);
}
```

* **Minimize work with memory**

If you don't measure efficiency of access to memory, efficiency of the CPU cache, efficiency of GC, you shouldn't create big arrays and you shouldn't allocate big amount of memory. For example, you want to measure performance of `ConvertAll(x => 2 * x).ToList()`. You can write code like this:

```cs
List<int> list = /* ??? */;
public List<int> ConvertAll()
{
    return list.ConvertAll(x => 2 * x).ToList();
}
```

In this case, you should create a small list like this:

```cs
List<int> list = new List<int> { 1, 2, 3, 4, 5 };
```

If you create a big list (with millions of elements), then you will also measure efficiency of the CPU cache because you will have big amount of [cache miss](http://en.wikipedia.org/wiki/CPU_cache#Cache_miss) during the calculation.  

* **Power settings and other applications**

    * Turn off all of the applications except the benchmark process and the standard OS processes. If you run benchmark and work in the Visual Studio at the same time, it can negatively affect to benchmark results.
    * If you use laptop for benchmarking, keep it plugged in and use the maximum performance mode.

## How to run?

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
var summary = BenchmarkRUnner.RunSource(benchmarkSource);
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


## How it works?

* `BenchmarkRunner` generates an isolated project per each benchmark method/job/params and builds it in Release mode.

* Next, we take each method/job/params combination and try to measure its performance by launching benchmark process several times (`LaunchCount`).

* An invocation of the target method is an *operation*. A bunch of operation is an *iteration*. If you have a `Setup` method, it will be invoked before each iteration, but not between operations. We have the following type of iterations:

    * `Pilot`: The best operation count will be chosen.
    * `IdleWarmup`, `IdleTarget`: BenchmarkDotNet overhead will be evaluated.
    * `MainWarmup`: Warmup of the main method.
    * `MainTarget`: Main measurements.
    * `Result` = `MainTarget` - `<AverageOverhead>`

* After all of the measurements, BenchmarkDotNet creates:

    * An instance of the `Summary` class that contains all information about benchmark runs.
    * A set of files that contains summary in human-readable and machine-readable formats.
    * A set of plots.

## FAQ

**Question** Benchmarks takes a lot of time, how I can speedup it?

**Answer** In general case, you need a lot of time for achieving good accuracy. If you are sure that you don't have any tricky performance effects and you don't need such level of accuracy, you can create a special Job. An example:

```cs
public class FastAndDirtyConfig : ManualConfig
{
    public FastAndDirtyConfig()
    {
        Add(Job.Default
            .WithLaunchCount(1)     // benchmark process will be launched only once
            .WithIterationTime(100) // 100ms per iteration
            .WithWarmupCount(3)     // 3 warmup iteration
            .WithTargetCount(3)     // 3 target iteration
        );
    }
}
```

## Team

Authors: [Andrey Akinshin](https://github.com/AndreyAkinshin) (maintainer), [Jon Skeet](https://github.com/jskeet), [Matt Warren](https://github.com/mattwarren)

Contributors: [Adam Sitnik](https://github.com/adamsitnik), [Sasha Goldshtein](https://github.com/goldshtn), and [others](https://github.com/PerfDotNet/BenchmarkDotNet/graphs/contributors)

2013–2016
