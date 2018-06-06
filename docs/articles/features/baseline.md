# Baseline

In order to scale your results, you can mark a benchmark method or a job as a baseline.
Let's learn this feature by examples.

## Example 1: Methods

You can mark a method as a baseline with the help of `[Benchmark(Baseline = true)]`. 

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

As a result, you will have additional `Scaled` column in the summary table:

```ini
BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.192)
Processor=Intel Core i7-6700HQ CPU 2.60GHz (Skylake), ProcessorCount=8
Frequency=2531249 Hz, Resolution=395.0619 ns, Timer=TSC
.NET Core SDK=2.0.3
  [Host]     : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
```

|  Method |      Mean |     Error |    StdDev | Scaled |
|-------- |----------:|----------:|----------:|-------:|
|  Time50 |  50.46 ms | 0.0779 ms | 0.0729 ms |   0.50 |
| Time100 | 100.39 ms | 0.0762 ms | 0.0713 ms |   1.00 |
| Time150 | 150.48 ms | 0.0986 ms | 0.0922 ms |   1.50 |

 
## Example 2: Methods with categories

The only way to have several baselines in the same class is to separate them by categories
  and mark the class with `[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]`.
  
```cs
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Sleeps
{
    [BenchmarkCategory("Fast"), Benchmark(Baseline = true)]        
    public void Time50() => Thread.Sleep(50);

    [BenchmarkCategory("Fast"), Benchmark]
    public void Time100() => Thread.Sleep(100);
    
    [BenchmarkCategory("Slow"), Benchmark(Baseline = true)]        
    public void Time550() => Thread.Sleep(550);

    [BenchmarkCategory("Slow"), Benchmark]
    public void Time600() => Thread.Sleep(600);
}
```

```ini
BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.192)
Processor=Intel Core i7-6700HQ CPU 2.60GHz (Skylake), ProcessorCount=8
Frequency=2531249 Hz, Resolution=395.0619 ns, Timer=TSC
.NET Core SDK=2.0.3
  [Host]     : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
```

|  Method | Categories |      Mean |     Error |    StdDev | Scaled |
|-------- |----------- |----------:|----------:|----------:|-------:|
|  Time50 |       Fast |  50.46 ms | 0.0745 ms | 0.0697 ms |   1.00 |
| Time100 |       Fast | 100.47 ms | 0.0955 ms | 0.0893 ms |   1.99 |
|         |            |           |           |           |        |
| Time550 |       Slow | 550.48 ms | 0.0525 ms | 0.0492 ms |   1.00 |
| Time600 |       Slow | 600.45 ms | 0.0396 ms | 0.0331 ms |   1.09 |


## Example 3: Jobs

If you want to compare several runtime configuration,
  you can mark one of your jobs with `isBaseline = true`.

```cs
[ClrJob(isBaseline: true)]
[MonoJob]
[CoreJob]
public class RuntimeCompetition
{
    [Benchmark]
    public int SplitJoin() => string.Join(",", new string[1000]).Split(',').Length;
}
```

```ini
BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.192)
Processor=Intel Core i7-6700HQ CPU 2.60GHz (Skylake), ProcessorCount=8
Frequency=2531249 Hz, Resolution=395.0619 ns, Timer=TSC
.NET Core SDK=2.0.3
  [Host]     : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  Job-MXFYPZ : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2600.0
  Core       : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  Mono       : Mono 5.4.0 (Visual Studio), 64bit 
```

    Method | Runtime |     Mean |     Error |    StdDev | Scaled | ScaledSD |
---------- |-------- |---------:|----------:|----------:|-------:|---------:|
 SplitJoin |     Clr | 19.42 us | 0.2447 us | 0.1910 us |   1.00 |     0.00 |
 SplitJoin |    Core | 13.00 us | 0.2183 us | 0.1935 us |   0.67 |     0.01 |
 SplitJoin |    Mono | 39.14 us | 0.7763 us | 1.3596 us |   2.02 |     0.07 |
 
 