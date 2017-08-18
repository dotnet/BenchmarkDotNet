# Baseline

In order to scale your results, you need to mark one of your benchmark methods as a baseline. Only one method in class can have `Baseline = true` applied.

## Example

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

