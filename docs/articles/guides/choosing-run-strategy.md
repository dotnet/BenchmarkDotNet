---
#cspell:ignore runstrategy
uid: docs.runstrategy
name: Choosing RunStrategy
---

# Choosing RunStrategy

If you run a benchmark, you always (explicitly or implicitly) use a [job](xref:docs.jobs).
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
[SimpleJob(launchCount: 3, warmupCount: 10, iterationCount: 30)]
public class MyBenchmarkClass
```

---

[!include[IntroColdStart](../samples/IntroColdStart.md)]

[!include[IntroMonitoring](../samples/IntroMonitoring.md)]
