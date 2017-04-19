# Diagnosers

A **diagnoser** can attach to your benchmark and get some useful info.

The current Diagnosers are:

- GC and Memory Allocation (`MemoryDiagnoser`) which is cross platform, built-in and **is not enabled by default anymore**. Please see Adam Sitnik's [blog post](http://adamsitnik.com/the-new-Memory-Diagnoser/) for all the details.
- JIT Inlining Events (`InliningDiagnoser`). You can find this diagnoser in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`): [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/)
- Hardware Counter Diagnoser. You can find this diagnoser in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`): [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/).  Please see Adam Sitnik's [blog post](http://adamsitnik.com/Hardware-Counters-Diagnoser/) for all the details.


## Examples

Below is a sample output from the `GC and Memory Allocation` diagnoser, note the extra columns on the right-hand side ("Gen 0", "Gen 1", "Gen 2" and "Allocated"):

```
           Method |        Mean |     StdErr |      StdDev |      Median |  Gen 0 | Allocated |
----------------- |------------ |----------- |------------ |------------ |------- |---------- |
 'new byte[10kB]' | 884.4896 ns | 46.3528 ns | 245.2762 ns | 776.4237 ns | 0.1183 |     10 kB |
 ```

A config example:

```cs
private class Config : ManualConfig
{
    public Config()
    {
        Add(MemoryDiagnoser.Default);
        Add(new InliningDiagnoser());
    }
}
```

You can also use one of the following attributes (apply it on a class that contains Benchmarks):
```cs
[MemoryDiagnoser]
[InliningDiagnoser]
```

## Hardware Counters Diagnoser

This diagnoser is not enabled in explicit way as the other diagnosers. You need to specify `[HardwareCounters]` and we choose the right diagnoser in the runtime.

```cs
[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
public class Cpu_BranchPerdictor
{
    private const int N = 32767;
    private readonly int[] sorted, unsorted;

    public Cpu_BranchPerdictor()
    {
        var random = new Random(0);
        unsorted = new int[N];
        sorted = new int[N];
        for (int i = 0; i < N; i++)
            sorted[i] = unsorted[i] = random.Next(256);
        Array.Sort(sorted);
    }

    private static int Branch(int[] data)
    {
        int sum = 0;
        for (int i = 0; i < N; i++)
            if (data[i] >= 128)
                sum += data[i];
        return sum;
    }

    private static int Branchless(int[] data)
    {
        int sum = 0;
        for (int i = 0; i < N; i++)
        {
            int t = (data[i] - 128) >> 31;
            sum += ~t & data[i];
        }
        return sum;
    }

    [Benchmark]
    public int SortedBranch() => Branch(sorted);

    [Benchmark]
    public int UnsortedBranch() => Branch(unsorted);

    [Benchmark]
    public int SortedBranchless() => Branchless(sorted);

    [Benchmark]
    public int UnsortedBranchless() => Branchless(unsorted);
}
```

and the results:

```
 |             Method |        Mean | Mispredict rate | BranchInstructions/Op | BranchMispredictions/Op |
 |------------------- |------------ |---------------- |---------------------- |------------------------ |
 |       SortedBranch |  21.4539 us |           0,04% |                 70121 |                      24 |
 |     UnsortedBranch | 136.1139 us |          23,70% |                 68788 |                   16301 |
 |   SortedBranchless |  28.6705 us |           0,06% |                 35711 |                      22 |
 | UnsortedBranchless |  28.9336 us |           0,05% |                 35578 |                      17 |
```

## Restrictions

* In order to not affect main results we perform a separate run if any diagnoser is used. That's why it might take more time to execute benchmarks.
* MemoryDiagnoser:
	* Mono currently [does not](http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono) expose any api to get the number of allocated bytes. That's why our Mono users will get `?` in Allocated column.
	* In order to get the number of allocated bytes in cross platform way we are using `GC.GetAllocatedBytesForCurrentThread` which recently got [exposed](https://github.com/dotnet/corefx/pull/12489) for netcoreapp1.1. That's why BenchmarkDotNet does not support netcoreapp1.0 from version 0.10.1.
	* MemoryDiagnoser is `99.5%` accurate about allocated memory when using default settings or Job.ShortRun (or any longer job than it).
* HardwareCounters:
	* Windows 8+ only (we plan to add Unix support in the future)
    * No Hyper-V (Virtualization) support
    * Requires to run as Admin (ETW Kernel Session)
    * No `InProcessToolchain` support ([#394](https://github.com/dotnet/BenchmarkDotNet/issues/394))
