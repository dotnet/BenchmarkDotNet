---
uid: BenchmarkDotNet.Samples.IntroJitStatsDiagnoser
---

## Sample: IntroJitStatsDiagnoser

This diagnoser shows various stats from the JIT compiler that were collected during entire benchmark run (warmup phase and BenchmarkDotNet-generated boilerplate code are included):
* Amount of JITted methods.
* Amount of [tiered methods](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#tiered-compilation).
* How much memory JIT allocated during the benchmark.

### Restrictions

* Windows only

### Source code

[!code-csharp[IntroJitStatsDiagnoser.cs](../../../samples/BenchmarkDotNet.Samples/IntroJitStatsDiagnoser.cs)]

### Output

| Method |     Mean |    Error |   StdDev | Methods JITted | Methods Tiered | JIT allocated memory |
|------- |---------:|---------:|---------:|---------------:|---------------:|---------------------:|
|  Sleep | 15.50 ms | 0.052 ms | 0.048 ms |          1,102 |            214 |            221,736 B |

### Links

* @docs.diagnosers
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroJitStatsDiagnoser

---