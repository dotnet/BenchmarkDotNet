**BenchmarkDotNet** is a lightweight .NET library for benchmarking, it helps you to create accurate benchmarks in an easy way.

[![NuGet version](https://badge.fury.io/nu/BenchmarkDotNet.svg)](https://badge.fury.io/nu/BenchmarkDotNet) [![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [Wiki](https://github.com/PerfDotNet/BenchmarkDotNet/wiki)

## Features
* BenchmarkDotNet creates an isolated project for each benchmark method and automatically runs it in a separate runtime, in Release mode, without an attached debugger.
* You can create benchmark tasks that run your benchmark with different CLR, JIT and platform versions.
* BenchmarkDotNet performs warm-up executions of your code, then runs it several times in different CLR instances, calculates statistics and tries to eliminate some runtime side-effects.
* BenchmarkDotNet runs with minimal overhead so as to give an accurate performance measurment.

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
new BenchmarkRunner().Run<Md5VsSha256>();
```

**Step 4** View the results, here is an example of output from the above benchmark:

```ini
BenchmarkDotNet-Dev=v0.7.8.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4702MQ CPU @ 2.20GHz, ProcessorCount=8
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit  [RyuJIT]
Type=Algo_Md5VsSha256  Mode=Throughput  Platform=HostPlatform  Jit=HostJit  .NET=HostFramework  toolchain=Classic  Runtime=Clr  Warmup=5  Target=10
```

| Method |     AvrTime |    StdDev |      op/s |
|------- |------------ |---------- |---------- |
|    Md5 |  26.2220 us | 0.2254 us | 38,138.59 |
| Sha256 | 139.7358 us | 9.2690 us |  7,183.93 |

## Advanced Features

### Attributes
BenchmarkDotNet provides you with several features that let you write more complex and powerful benchmarks.

- `[BenchmarkTask]` attribute let you specify different useful variables: jit version (Legacy/RyuJIT), cpu architecture (x86/x64), target runtime (CLR/Mono), and so on. You can use several attributes to compare different benchmark configurations.
- `[OperationsPerInvoke]` attribute let you create a benchmark that contains specific amount of operation. It can be very useful for really quick operations that hard to measure: you can merge them into a single method.
- `[Setup]` attribute let you specify a method that can be run before each benchmark *batch* or *run*
- `[Params(..)]` makes it easy to run the same benchmark with different input values

The code below shows how these features can be used. In this example the benchmark will be run 4 times, with the value of `MaxCounter` automaticially initialised each time to the values [`1, 5, 10, 100`]. In addition before each run the `SetupData()` method will be called, so that `initialValuesArray` can be re-sized based on `MaxCounter`.

```cs
[BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
[BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
[BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
public class IL_Loops
{
    [Params(1, 5, 10, 100)]
    int MaxCounter = 0;
    
    private int[] initialValuesArray;

    [Setup]
    public void SetupData()
    {
        initialValuesArray = Enumerable.Range(0, MaxCounter).ToArray();
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
    public int ForEachArray()
    {
        var counter = 0;
        foreach (var i in initialValuesArray)
            counter += i;
        return counter;
    }
}
```

### Alternative ways of executing Benchmarks

You can also run a benchmark directly from the internet:

```cs
string url = "https://raw.githubusercontent.com/PerfDotNet/BenchmarkDotNet/master/BenchmarkDotNet.Samples/CPU/Cpu_Ilp_Inc.cs";
new BenchmarkRunner().RunUrl(url);
```

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

### Export

There are some preformatted md files that you can use to share benchmark results in the net. If you have a default plugins set they will be located in your bin directory. There are markdown files for StackOverflow and GitHub. They can be easily found by specific file suffix:

```
<BenchmarkName>-report.csv
<BenchmarkName>-runs.csv

<BenchmarkName>-report-default.md
<BenchmarkName>-report-github.md
<BenchmarkName>-report-stackoverflow.md

<BenchmarkName>-report.txt
```

### Plots

If you have installed [R](https://www.r-project.org/) and defined `%R_HOME%` variable, you will also get nice barplots and boxplots via `BenchmarkRPlotExporter` that generates `BuildPlots.R` in your bin directory.

## Authors

Andrey Akinshin, Jon Skeet, Matt Warren