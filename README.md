**BenchmarkDotNet** is a lightweight .NET library for benchmarking. [Microbenchmarking is very hard](https://andreyakinshin.gitbooks.io/performancebookdotnet/content/science/microbenchmarking.html), but BenchmarkDotNet helps you to create accurate benchmarks in an easy way.

## Features
* BenchmarkDotNet creates an isolated project for each benchmark method and automatically runs it in a separate runtime, in Release mode, without an attached debugger.
* You can create benchmark tasks that run your benchmark with different CLR, JIT and platform versions.
* BenchmarkDotNet performs warm-up executions of your code, then runs it several times in different CLR instances, calculates statistics and tries to eliminate some runtime side-effects.
* BenchmarkDotNet runs with minimal overhead so as to give an accurate performance measurment.
 
## Chat Room
[![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Getting started

**Step 1** Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

**Step 2** Write a class with methods that you want to measure and mark them with the `Benchmark` attribute. You can also use additional attributes like `OperationsPerInvoke` (amount of operations in your method) or `BenchmarkTask` (specify the benchmark environment). In the following example, we will research how [Instruction-level parallelism](http://en.wikipedia.org/wiki/Instruction-level_parallelism) affects application performance:

```cs
[BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
[BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
[BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
public class Cpu_Ilp_Inc
{
    private double a, b, c, d;

    [Benchmark]
    [OperationsPerInvoke(4)]
    public void Parallel()
    {
        a++;
        b++;
        c++;
        d++;
    }

    [Benchmark]
    [OperationsPerInvoke(4)]
    public void Sequential()
    {
        a++;
        a++;
        a++;
        a++;
    }
}
```

**Step 3** Run it:

```cs
new BenchmarkRunner().RunCompetition(new Cpu_Ilp_Inc());
```

**Step 4** View the results, here is the output from the above benchmark:

```ini
BenchmarkDotNet=v0.7.7.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4702MQ CPU @ 2.20GHz, ProcessorCount=8
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit  [RyuJIT]
Type=Cpu_Ilp_Inc  Mode=Throughput  .NET=HostFramework
```

|     Method | Platform |       Jit |   AvrTime |    StdDev |             op/s |
|----------- |--------- |---------- |---------- |---------- |----------------- |
|   Parallel |      X64 | LegacyJit | 0.2856 ns | 0.0281 ns | 3,501,283,192.20 |
| Sequential |      X64 | LegacyJit | 2.6460 ns | 0.0513 ns |   377,932,149.37 |
|   Parallel |      X64 |    RyuJit | 0.2760 ns | 0.0094 ns | 3,622,785,633.08 |
| Sequential |      X64 |    RyuJit | 0.8576 ns | 0.0220 ns | 1,166,095,637.19 |
|   Parallel |      X86 | LegacyJit | 0.3743 ns | 0.0081 ns | 2,671,346,912.81 |
| Sequential |      X86 | LegacyJit | 3.0057 ns | 0.0664 ns |   332,699,953.00 |

## Advanced Features
BenchmarkDotNet provieds you with several features that let you write more complex and powerful benchmarks. 

- `[Setup]` attribute let you specify a method that can be run before each benchmark *batch* or *run*
- `[Params(..)]` makes it easy to run the same benchmark with different input values
 
The code below shows how these features can be used. In this example the benchmark will be run 4 times, with the value of `MaxCounter` automaticially initialised each time to the values [`1, 5, 10, 100`]. In addition before each run the `SetupData()` method will be called, so that `initialValuesArray` can be re-sized based on `MaxCounter`.

``` csharp
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
new BenchmarkRunner().RunUrl(
  "https://raw.githubusercontent.com/PerfDotNet/BenchmarkDotNet/master/BenchmarkDotNet.Samples/CPU/Cpu_Ilp_Inc.cs");
```

Or you can create a set of benchmarks and choose one from command line. Here is set of benchmarks from the [BenchmarkDotNet.Samples](https://github.com/PerfDotNet/BenchmarkDotNet/tree/master/BenchmarkDotNet.Samples) project:

```cs
var competitionSwitch = new BenchmarkCompetitionSwitch(new[] {
    // Introduction
    typeof(Intro_00_Basic),
    typeof(Intro_01_MethodTasks),
    typeof(Intro_02_ClassTasks),
    typeof(Intro_03_SingleRun),
    typeof(Intro_04_UniformReportingTest),
    // IL
    typeof(Il_ReadonlyFields),
    typeof(Il_Switch),
    // JIT
    typeof(Jit_LoopUnrolling),
    typeof(Jit_ArraySumLoopUnrolling),
    typeof(Jit_Inlining),
    typeof(Jit_BoolToInt),
    typeof(Jit_Bce),
    typeof(Jit_InterfaceMethod),
    typeof(Jit_RegistersVsStack),
    // CPU
    typeof(Cpu_Ilp_Inc),
    typeof(Cpu_Ilp_Max),
    typeof(Cpu_Ilp_VsBce),
    typeof(Cpu_Ilp_RyuJit),
    typeof(Cpu_MatrixMultiplication),
    typeof(Cpu_BranchPerdictor),
    // Framework
    typeof(Framework_SelectVsConvertAll),
    typeof(Framework_StackFrameVsStackTrace),
    typeof(Framework_StopwatchVsDateTime),
    // Algorithms
    typeof(Algo_BitCount),
    typeof(Algo_MostSignificantBit),
    typeof(Algo_Md5VsSha256),
    // Other
    typeof(Math_DoubleSqrt),
    typeof(Math_DoubleSqrtAvx),
    typeof(Array_HeapAllocVsStackAlloc),
    // Infra
    typeof(Infra_Params)
});
competitionSwitch.Run(args);
```

## Future plans

* In-line IL and ASM viewer
* Graphing results
* Mono/CoreCLR/.NET Native support
* .NET 2.0 support
* Hardware analysis
* Multithreading support
* Infrastructure improvements

## Authors

Andrey Akinshin, Jon Skeet, Matt Warren
