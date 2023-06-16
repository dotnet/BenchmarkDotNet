---
uid: BenchmarkDotNet.Samples.IntroDotTraceDiagnoser
---

## Sample: IntroDotTraceDiagnoser

If you want to get a performance profile of your benchmarks, just add the `[DotTraceDiagnoser]` attribute, as shown below.
As a result, BenchmarkDotNet performs bonus benchmark runs using attached
  [dotTrace Command-Line Profiler](https://www.jetbrains.com/help/profiler/Performance_Profiling__Profiling_Using_the_Command_Line.html).
The obtained snapshots are saved to the `artifacts` folder.
These snapshots can be opened using the [standalone dotTrace](https://www.jetbrains.com/profiler/),
  or [dotTrace in Rider](https://www.jetbrains.com/help/rider/Performance_Profiling.html).

### Source code

[!code-csharp[IntroDotTraceDiagnoser.cs](../../../samples/BenchmarkDotNet.Samples/IntroDotTraceDiagnoser.cs)]

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroDotTraceDiagnoser

---