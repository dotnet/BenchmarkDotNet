---
uid: BenchmarkDotNet.Samples.IntroThreadingDiagnoser
---

## Sample: IntroThreadingDiagnoser

The `ThreadingDiagnoser` uses new APIs [exposed](https://github.com/dotnet/corefx/issues/35500) in .NET Core 3.0 to report:

* Completed Work Items: The number of work items that have been processed in ThreadPool (per single operation)
* Lock Contentions: The number of times there **was contention** upon trying to take a Monitor's lock (per single operation)

### Source code

[!code-csharp[IntroThreadingDiagnoser.cs](../../../samples/BenchmarkDotNet.Samples/IntroThreadingDiagnoser.cs)]

### Output

|              Method |          Mean |     StdDev |        Median | Completed Work Items | Lock Contentions |
|-------------------- |--------------:|-----------:|--------------:|---------------------:|-----------------:|
| CompleteOneWorkItem | 8,073.5519 ns | 69.7261 ns | 8,111.6074 ns |               1.0000 |                - |

### Links

* @docs.diagnosers
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroThreadingDiagnoser

---