**BenchmarkDotNet** is a .NET library for benchmarking, it helps you to create accurate benchmarks in an easy way.

[![NuGet version](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) [![NuGet downloads](https://img.shields.io/nuget/dt/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) [![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://img.shields.io/gitter/room/PerfDotNet/BenchmarkDotNet.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet)

Wiki: [Home](https://github.com/PerfDotNet/BenchmarkDotNet/wiki), [ChangeLog](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/ChangeLog), [Developing](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/Developing), [Roadmap](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/Roadmap)

Content:

* [Getting started](#getting-started)
* [Configs](#configs)
* [Advanced features](#advanced-features)
* [How to run?](#how-to-run)
* [How it works?](#how-it-works)
* [Authors](#authors)

## Getting started

**Step 1** Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```
PM> Install-Package BenchmarkDotNet
```

**Step 2** Write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we will compare [MD5](https://en.wikipedia.org/wiki/MD5) and [SHA256](https://en.wikipedia.org/wiki/SHA-2) cryptographic hash functions:

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

**Step 3** Run it:

```cs
var summary = BenchmarkRunner.Run<Md5VsSha256>();
```

**Step 4** View the results, here is an example of output from the above benchmark:

```ini
BenchmarkDotNet-Dev=v0.8.2.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4810MQ CPU @ 2.80GHz, ProcessorCount=8
Freq=2728058 ticks, Resolution=366.5611 ns [HighResolution]
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]

Type=Algo_Md5VsSha256  Mode=Throughput  Platform=HostPlatform  
Jit=HostJit  .NET=HostFramework  toolchain=Classic  
Runtime=Clr  Warmup=5  Target=10  

```
 Method |     AvrTime |     Error
------- |------------ |----------
    Md5 |  21.0502 us | 0.0442 us
 Sha256 | 118.5298 us | 1.2863 us

**Step 5** Analyze it. In your bin directory, you can find a lot of useful files with detailed information. For example:
  * Csv reports with raw data: `Md5VsSha256-report.csv`, `Md5VsSha256-runs.csv`
  * Markdown reports:  `Md5VsSha256-report-default.md`, `Md5VsSha256-report-stackoverflow.md`, `Md5VsSha256-report-github.md`
  * Plain report and log: `Md5VsSha256-report.txt`, `Md5VsSha256.log`
  * Plots (if you have installed R): `Md5VsSha256-barplot.png`, `Md5VsSha256-boxplot.png`

## Configs

Config is a set of so called `jobs`, `columns`, `exporters`, `loggers`, `diagnosers`, `analysers` that help you to build your benchmark. There are two ways to set your config:

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

### Jobs

A *job* is an environment for your benchmarks. You can set one or several jobs for your set of benchmarks.

Job characteristics:

* **Toolchain** A toolchain for generating/building/executing your benchmark. Values: `Classic` (csproj based) *(default)*. Coming soon: `Dnx`.
* **Mode** Values: `Throughput` *(default)*, `SingleRun`
* **Platform** Values: `Host` *(default)*, `AnyCpu`, `X86`, X64`
* **Jit** Values: `Host` *(default)*, `LegacyJit`, `RyuJit`
* **Framework** Values: `Host` *(default)*, `V40`, `V45`, `V451`, `V452`, `V46`
* **Runtime** Values: `Host` *(default)*, `Clr`, `Mono`. Coming soon: `CoreClr`.
* **ProcessCount** Values: `Auto` *(default)* or specific number.
* **WarmupCount** Values: `Auto` *(default)* or specific number.
* **TargetCount** Values: `Auto` *(default)* or specific number.
* **Affinity** [ProcessorAffinity](https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx). Values: `Auto` *(default)* or specific mask.

The `Host` value means that value will be resolved from host process settings. The `Auto` values means the BenchmarkDotNet automatically choose the best value.

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
    IColumn Time;
    IColumn Error;
    IColumn StdDev;
    IColumn OperationPerSecond;
    IColumn Min;
    IColumn Q1;
    IColumn Median;
    IColumn Q3;
    IColumn Max;
    IColumn[] AllStatistics = { Time, Error, StdDev, OperationPerSecond, Min, Q1, Median, Q3, Max };}
}
class Place
{
    IColumn ArabicNumber;
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
    IColumn ProcessCount;
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
* `StatisticColumn.Time`
* `StatisticColumn.Error`
* `BaselineDeltaColumn.Default`
* `PlaceColumn`: specify "place" of each benchmark. Place 1 means a group of the fastest benchmarks, place 2 means the second group, and so on. There are several styles:
  * `PlaceColumn.ArabicNumber`: `1`, `2`, `3`, ...
  * `PlaceColumn.Stars`: `*`, `**`, `***`, ...

** Examples **

```cs
// *** Command style ***
[Config("columns=Min,Max")]
[Config("columns=AllStatistics")]
```

```cs
// *** Object style ***
// You can add custom tags per each method using Columns
[Config(typeof(Config))]
public class IntroTags
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(Job.Dry);
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
//  Method |       Time |     Error | Foo or Bar | Number |
// ------- |----------- |---------- |----------- |------- |
//   Bar34 | 10.3636 ms | 0.0000 ms |        Bar |     34 |
//    Bar3 | 10.4662 ms | 0.0000 ms |        Bar |      3 |
//   Foo12 | 10.1377 ms | 0.0000 ms |        Foo |     12 |
//    Foo1 | 10.2814 ms | 0.0000 ms |        Foo |      1 |
```

### Exporters

TODO

There are some preformatted md files that you can use to share benchmark results in the net. If you have a default plugins set they will be located in your bin directory. There are markdown files for StackOverflow and GitHub. They can be easily found by specific file suffix:

```
<BenchmarkName>-report.csv
<BenchmarkName>-runs.csv

<BenchmarkName>-report-default.md
<BenchmarkName>-report-github.md
<BenchmarkName>-report-stackoverflow.md

<BenchmarkName>-report.txt
```

**Plots** If you have installed [R](https://www.r-project.org/) and defined `%R_HOME%` variable, you will also get nice barplots and boxplots via `BenchmarkRPlotExporter` that generates `BuildPlots.R` in your bin directory.

### Loggers

TODO

### Diagnosers

TODO

### Analysers

TODO

## Advanced Features

### Params

TODO

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

   Method  |        Time |     Error |   A |  B
---------- |------------ |---------- |---- |---
 Benchmark | 115.3325 ms | 0.0242 ms | 100 | 10
 Benchmark | 125.3282 ms | 0.0245 ms | 100 | 20
 Benchmark | 215.3024 ms | 0.0375 ms | 200 | 10
 Benchmark | 225.2710 ms | 0.0434 ms | 200 | 20

### OperationsPerInvoke

TODO

### Setup

TODO

### Baseline

You can mark one of your benchmark method as a baseline:

```cs
public class Intro_08_Baseline
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
BenchmarkDotNet=v0.8.2.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4810MQ CPU @ 2.80GHz, ProcessorCount=8
Freq=2728058 ticks, Resolution=366.5611 ns [HighResolution]
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]

Type=Intro_08_Baseline  Mode=Throughput  Platform=HostPlatform  
Jit=HostJit  .NET=HostFramework  toolchain=Classic  
Runtime=Clr  Warmup=5  Target=10  
```

  Method |     AvrTime |     Error | +/- Delta
-------- |------------ |---------- |----------
 Time100 | 100.4055 ms | 0.1595 ms |         -
 Time150 | 150.2872 ms | 0.0231 ms |     49.7%
  Time50 |  50.1891 ms | 0.0108 ms |    -50.0%



### How to run?

There are several ways to run your benchmarks.

#### Types

TODO

#### Url

You can also run a benchmark directly from the internet:

```cs
string url = "https://raw.githubusercontent.com/PerfDotNet/BenchmarkDotNet/master/BenchmarkDotNet.Samples/CPU/Cpu_Ilp_Inc.cs";
new BenchmarkRunner().RunUrl(url);
```

#### Source

TODO

#### BenchmarkSwitcher

Or you can create a set of benchmarks and choose one from command line:

```cs
var benchmarkSwitcher = new BenchmarkSwitcher(new[] {
    typeof(Intro_00_Basic),
    typeof(Intro_01_MethodTasks),
    typeof(Intro_02_ClassTasks),
    typeof(Intro_03_SingleRun),
    typeof(Intro_04_UniformReportingTest),
});
benchmarkSwitcher.Run(args);
```

#### Command line

TODO


## How it works?

TODO
* BenchmarkDotNet creates an isolated project for each benchmark method and automatically runs it in a separate runtime, in Release mode, without an attached debugger.
* You can create benchmark tasks that run your benchmark with different CLR, JIT and platform versions.
* BenchmarkDotNet performs warm-up executions of your code, then runs it several times in different CLR instances, calculates statistics and tries to eliminate some runtime side-effects.
* BenchmarkDotNet runs with minimal overhead so as to give an accurate performance measurment.

## Authors

Andrey Akinshin, Jon Skeet, Matt Warren (2013–2016)