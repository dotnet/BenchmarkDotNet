---
uid: BenchmarkDotNet.Samples.IntroNuGet
---

## Sample: IntroNuGet

You can set specific versions of NuGet dependencies for each job using MsBuild properties in your csproj.
It allows comparing different versions of the same package (if there are no breaking changes in API).

### Source code

[!code-csharp[IntroNuGet.cs](../../../samples/BenchmarkDotNet.Samples/IntroNuGet.cs)]

### Output

| Method                    | Job    | Arguments           | Mean     | Error     | StdDev    |
|-------------------------- |------- |-------------------- |---------:|----------:|----------:|
| ToImmutableArrayBenchmark | v9.0.0 | /p:SciVersion=9.0.0 | 1.173 μs | 0.0057 μs | 0.0086 μs |
| ToImmutableArrayBenchmark | v9.0.3 | /p:SciVersion=9.0.3 | 1.173 μs | 0.0038 μs | 0.0058 μs |
| ToImmutableArrayBenchmark | v9.0.5 | /p:SciVersion=9.0.5 | 1.172 μs | 0.0107 μs | 0.0157 μs |

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroNuGet

---