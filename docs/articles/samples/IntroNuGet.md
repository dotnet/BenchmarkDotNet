---
uid: BenchmarkDotNet.Samples.IntroNuGet
---

## Sample: IntroNuGet

You can set specific versions of NuGet dependencies for each job using MsBuild properties in your csproj.
It allows comparing different versions of the same package (if there are no breaking changes in API).

### Source code

[!code-csharp[IntroNuGet.cs](../../../samples/BenchmarkDotNet.Samples/IntroNuGet.cs)]

### Output

| Method                   | Job     | Arguments                       | Mean     | Error    | StdDev   |
|------------------------- |-------- |-------------------------------- |---------:|---------:|---------:|
| SerializeAnonymousObject | v13.0.1 | /p:NewtonsoftJsonVersion=13.0.1 | 652.7 ns | 10.68 ns | 15.98 ns |
| SerializeAnonymousObject | v13.0.2 | /p:NewtonsoftJsonVersion=13.0.2 | 654.0 ns |  8.62 ns | 12.89 ns |
| SerializeAnonymousObject | v13.0.3 | /p:NewtonsoftJsonVersion=13.0.3 | 678.6 ns | 17.62 ns | 26.38 ns |
| SerializeAnonymousObject | v13.0.4 | /p:NewtonsoftJsonVersion=13.0.4 | 637.2 ns | 16.95 ns | 24.84 ns |

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroNuGet

---