**BenchmarkDotNet** is a powerful .NET library for benchmarking.

**Links:** [NuGet](https://www.nuget.org/packages/BenchmarkDotNet/), [Wiki](https://github.com/PerfDotNet/BenchmarkDotNet/wiki), [ChangeLog](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/ChangeLog), [Developing](DEVELOPING.md)

[![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://img.shields.io/gitter/room/PerfDotNet/BenchmarkDotNet.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet)

**Summary**

* Standard benchmarking routine: generating an isolated project per each benchmark method; autoselection of iteration amount; warmup; overhead evaluation; statistics calucalation; and so on.
* Easy way to compare different environments (`x86` vs `x64`, `LegacyJit` vs `RyuJit`, and so on; see: [Jobs](#jobs))
* Reports: markdown (default, github, stackoverflow), csv, html, plain text; png plots.
* Advanced features: [Baseline](#baseline), [Params](#params)
* Powerful diagnostics based on ETW events (currently, works only from source)
* Supported: Full .NET Framework, Mono, Dnx451, DnxCore50 (in prerelease version)

## Content

* [Getting started](#getting-started)
* [Configs](#configs)
* [Advanced features](#advanced-features)
* [How to run?](#how-to-run)
* [How it works?](#how-it-works)
* [Team](#team)

## Getting started

**Step 1.** Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```
PM> Install-Package BenchmarkDotNet
```

If you want to use CoreCLR (`dnxcore50`), you need prerelease version of the package:

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

### Jobs

A *job* is an environment for your benchmarks. You can set one or several jobs for your set of benchmarks.

Job characteristics:

* **Toolchain.** A toolchain for generating/building/executing your benchmark. Values: `Classic` (csproj based) *[default]*. Coming soon: `Dnx`.
* **Mode.** Values: `Throughput` *[default]*, `SingleRun`.
* **Platform.** Values: `Host` *[default]*, `AnyCpu`, `X86`, `X64`.
* **Jit.** Values: `Host` *[default]*, `LegacyJit`, `RyuJit`.
* **Framework.** Values: `Host` *[default]*, `V40`, `V45`, `V451`, `V452`, `V46`.
* **Runtime.** Values: `Host` *[default]*, `Clr`, `Mono`. Coming soon: `CoreClr`.
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
    IColumn[] AllStatistics = { Time, Error, StdDev, OperationPerSecond, Min, Q1, Median, Q3, Max };}
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

An *exporter* allows you to export results of your benchmark in different formats. By default, files with results will be located in your bin directory. Here are some exporter examples:

* **Markdown**

```
<BenchmarkName>-report-default.md
<BenchmarkName>-report-github.md
<BenchmarkName>-report-stackoverflow.md
```

* **Csv**

```
<BenchmarkName>-report.csv
<BenchmarkName>-measurements.csv
```

* **Html**

```
<BenchmarkName>-report.html
```

* **Plain**

```
<BenchmarkName>-report.txt
```

* **Plots**

If you have installed [R](https://www.r-project.org/) and defined `%R_HOME%` variable, you will also get nice plots with help of the `BuildPlots.R` script in your bin directory. Examples:

```
<BenchmarkName>-barplot.png
<BenchmarkName>-boxplot.png
<BenchmarkName>-<MethodName>-density.png
<BenchmarkName>-<MethodName>-facetTimeline.png
<BenchmarkName>-<MethodName>-facetTimelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
```

### Loggers

A **logger** allows you to log results of your benchmark. By default, you can see log on console and in a file (`<BenchmarkName>.log`).

### Diagnosers

A **diagnoser** can attach to your benchmark and get some useful info. For now, there are no default diagnosers in the NuGet package. If you want to use it, you should build BenchmarkDotNet manually from the source code.

### Analysers

An **analyser** can analyze summary of your benchmarks and produce some useful warnings. For example, `EnvironmentAnalyser` warns you, if you build your application in the DEBUG mode or run it with an attached debugger.

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

If you have some data that you want to initialize, the `Setup` method is the best place for this:

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

You can mark one of your benchmark methods as a baseline:

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


### How to run?

There are several ways to run your benchmarks.

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

1. `BenchmarkRunner` generates an isolated project per each benchmark method/job/params, build it in Release mode and automatically runs it several time in the Release mode.

2. Each run consist from several stages:

    * `Pilot`: On this stage, the best iteration count will be choosen.
    * `IdleWarmup`, `IdleTarget`: On these stage, BenchmarkDotNet overhead will be evaluated
    * `MainWarmup`: Warmup of the main method.
    * `MainTarget`: Main measurements.
    * `Result` = `MainTarget` - `<AverageOverhead>`

3. After all of the runs, BenchmarkDotNet creates:

    * An instance of the `Summary` class that contains all information about benchmark runs.
    * Set of files that contains summary in human-readable and machine-readable formats.
    * Set of plots.

## Team

Authors: [Andrey Akinshin](https://github.com/AndreyAkinshin) (maintainer), [Jon Skeet](https://github.com/jskeet), [Matt Warren](https://github.com/mattwarren)
Contributors: [Adam Sitnik](https://github.com/adamsitnik), [Sasha Goldshtein](https://github.com/goldshtn), and [others](https://github.com/PerfDotNet/BenchmarkDotNet/graphs/contributors)

2013–2016