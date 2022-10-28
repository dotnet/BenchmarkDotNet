---
#cspell:ignore runstrategy
uid: BenchmarkDotNet.Samples.IntroMonitoring
---

## Sample: IntroMonitoring

If a benchmark method takes at least 100ms, you can also use the `Monitoring` strategy.
In this case, the pilot stage will be omitted, by default you get 1 iteration = 1 operation
  (or you can manually set amount of operation in an iteration).
Also you can use `[IterationSetup]` and `[IterationCleanup]` in this case: it shouldn't affect time measurements
  (but it can affect results of MemoryDiagnoser).
It's a perfect mode for benchmarks which doesn't have a steady state and the performance distribution is tricky:
  `Monitoring` will help you to collect a set of measurements and get statistics.

### Usage

```cs
[SimpleJob(RunStrategy.Monitoring, launchCount: 10, warmupCount: 0, iterationCount: 100)]
public class MyBenchmarkClass
```

### Source code

[!code-csharp[IntroMonitoring.cs](../../../samples/BenchmarkDotNet.Samples/IntroMonitoring.cs)]

### Output

```markdown
Result       1: 1 op, 61552600.00 ns, 61.5526 ms/op
Result       2: 1 op, 10141700.00 ns, 10.1417 ms/op
Result       3: 1 op, 10482900.00 ns, 10.4829 ms/op
Result       4: 1 op, 50410900.00 ns, 50.4109 ms/op
Result       5: 1 op, 10421400.00 ns, 10.4214 ms/op
Result       6: 1 op, 20556100.00 ns, 20.5561 ms/op
Result       7: 1 op, 70473200.00 ns, 70.4732 ms/op
Result       8: 1 op, 50581700.00 ns, 50.5817 ms/op
Result       9: 1 op, 10559000.00 ns, 10.5590 ms/op
Result      10: 1 op, 70496300.00 ns, 70.4963 ms/op
```

| Method |     Mean |    Error |   StdDev |      Min |       Q1 |       Q3 |      Max |
|------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|
|    Foo | 36.57 ms | 40.03 ms | 26.47 ms | 10.14 ms | 10.48 ms | 61.55 ms | 70.50 ms |

### Links

* @docs.runstrategy
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroMonitoring

---
