---
uid: BenchmarkDotNet.Samples.IntroHardwareCounters
---

## Sample: IntroHardwareCounters

This diagnoser is not enabled in explicit way as the other diagnosers.
You need to specify `[HardwareCounters]` and we choose the right diagnoser in the runtime.

### Source code

[!code-csharp[IntroHardwareCounters.cs](../../../samples/BenchmarkDotNet.Samples/IntroHardwareCounters.cs)]

### Output

|             Method |        Mean | Mispredict rate | BranchInstructions/Op | BranchMispredictions/Op |
|------------------- |------------ |---------------- |---------------------- |------------------------ |
|       SortedBranch |  21.4539 us |           0,04% |                 70121 |                      24 |
|     UnsortedBranch | 136.1139 us |          23,70% |                 68788 |                   16301 |
|   SortedBranchless |  28.6705 us |           0,06% |                 35711 |                      22 |
| UnsortedBranchless |  28.9336 us |           0,05% |                 35578 |                      17 |

### Links

* @docs.diagnosers
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroHardwareCounters

---