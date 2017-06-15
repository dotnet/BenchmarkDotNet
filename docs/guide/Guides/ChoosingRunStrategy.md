# Choosing RunStrategy

If you run a benchmark, you always (explicitly or implicitly) use a [job](http://benchmarkdotnet.org/Configs/Jobs.htm).
Each `Job` has the `RunStrategy` parameter which allows switching between different benchmark modes.
The default `RunStrategy` is `Throughput`, and it works fine for most cases.
However, other strategies are also useful in some specific cases.

## Throughput

`Throughput` is the default `RunStrategy`, works perfectly for microbenchmarking.
It's automatically choosing the amount of operation in main iterations based on a set of pilot iterations.
The amount of iterations will also be chosen automatically based on accuracy job settings.
A benchmark method should have a steady state.

Of course, you can manually set all the characteristics. An example:

```cs
[SimpleJob(launchCount: 3, warmupCount: 10, targetCount: 30)]
public class MyBenchmarkClass
```

## Monitoring

If a benchmark method takes at least 100ms, you can also use the `Monitoring` strategy.
In this case, the pilot stage will be omitted, by default you get 1 iteration = 1 operation (or you can manually set amount of operation in an iteration).
Also you can use `[IterationSetup]` and `[IterationCleanup]` in this case: it shouldn't affect time measurements (but it can affect results of MemoryDiagnoser).
It's a perfect mode for benchmarks which doesn't have a steady state and the performance distribution is tricky:
  `Monitoring` will help you to collect a set of measurements and get statistics.

```cs
[SimpleJob(RunStrategy.Monitoring, launchCount: 10, warmupCount: 0, targetCount: 100)]
public class MyBenchmarkClass
```

## ColdStart

If you want to measure cold start (without the pilot and warmup stage), the `ColdStart` strategy is your choice.

```cs
[SimpleJob(RunStrategy.ColdStart, launchCount:50)]
public class MyBenchmarkClass
```

