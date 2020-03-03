---
uid: BenchmarkDotNet.Samples.IntroNativeMemory
---

## Sample: IntroNativeMemory

The `NativeMemoryProfiler` uses `EtwProfiler` to profile the code using ETW and adds the extra columns `Allocated native memory` and `Native memory leak` to the benchmark results table.

### Source code

[!code-csharp[IntroNativeMemory.cs](../../../samples/BenchmarkDotNet.Samples/IntroNativeMemory.cs)]

### Output

|                Method |         Mean |         Error |       StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated | Allocated native memory | Native memory leak |
|---------------------- |-------------:|--------------:|-------------:|------:|------:|------:|----------:|------------------------:|-------------------:|
|       BitmapWithLeaks | 73,456.43 ns |  22,498.10 ns | 1,233.197 ns |     - |     - |     - |     177 B |                 13183 B |            11615 B |
|                Bitmap | 91,590.08 ns | 101,468.12 ns | 5,561.810 ns |     - |     - |     - |     180 B |                 12624 B |                  - |
|          AllocHGlobal |     79.91 ns |      43.93 ns |     2.408 ns |     - |     - |     - |         - |                    80 B |                  - |
| AllocHGlobalWithLeaks |    103.50 ns |     153.21 ns |     8.398 ns |     - |     - |     - |         - |                    80 B |               80 B |

### Profiling memory leaks

The BenchmarkDotNet repeats benchmarking function many times. Sometimes it can cause a memory overflow. In this case, the BenchmarkDotNet shows the message: 

```ini
OutOfMemoryException!
BenchmarkDotNet continues to run additional iterations until desired accuracy level is achieved. It's possible only if the benchmark method doesn't have any side-effects.
If your benchmark allocates memory and keeps it alive, you are creating a memory leak.
You should redesign your benchmark and remove the side-effects. You can use `OperationsPerInvoke`, `IterationSetup` and `IterationCleanup` to do that.
```

In this case, you should try to reduce the number of invocation, by adding `[ShortRunJob]` attribute or using `Job.Short` for custom configuration.

### Links

* @docs.diagnosers
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroNativeMemory

---