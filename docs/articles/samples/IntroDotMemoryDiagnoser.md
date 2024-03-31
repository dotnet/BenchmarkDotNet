---
uid: BenchmarkDotNet.Samples.IntroDotMemoryDiagnoser
---

## Sample: IntroDotMemoryDiagnoser

If you want to get a memory allocation profile of your benchmarks, just add the `[DotMemoryDiagnoser]` attribute, as shown below.
As a result, BenchmarkDotNet performs bonus benchmark runs using attached
  [dotMemory Command-Line Profiler](https://www.jetbrains.com/help/dotmemory/Working_with_dotMemory_Command-Line_Profiler.html).
The obtained dotMemory workspaces are saved to the `artifacts` folder.
These dotMemory workspaces can be opened using the [standalone dotMemory](https://www.jetbrains.com/dotmemory/),
  or [dotMemory in Rider](https://www.jetbrains.com/help/rider/Memory_profiling_of_.NET_code.html).

### Source code

[!code-csharp[IntroDotMemoryDiagnoser.cs](../../../samples/BenchmarkDotNet.Samples/IntroDotMemoryDiagnoser.cs)]

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroDotMemoryDiagnoser

---