---
uid: BenchmarkDotNet.Samples.IntroNuGet
---

## Sample: IntroNuGet

You can set specific versions of NuGet dependencies for each job.
It allows comparing different versions of the same package (if there are no breaking changes in API).

### Source code

[!code-csharp[IntroNuGet.cs](../../../samples/BenchmarkDotNet.Samples/IntroNuGet.cs)]

### Output

|                   Method |    Job |        NuGetReferences |     Mean |     Error |    StdDev |
|------------------------- |------- |----------------------- |---------:|----------:|----------:|
| SerializeAnonymousObject | 10.0.1 | Newtonsoft.Json 10.0.1 | 2.926 us | 0.0795 us | 0.0283 us |
| SerializeAnonymousObject | 10.0.2 | Newtonsoft.Json 10.0.2 | 2.877 us | 0.5928 us | 0.2114 us |
| SerializeAnonymousObject | 10.0.3 | Newtonsoft.Json 10.0.3 | 2.706 us | 0.1251 us | 0.0446 us |
| SerializeAnonymousObject | 11.0.1 | Newtonsoft.Json 11.0.1 | 2.778 us | 0.5037 us | 0.1796 us |
| SerializeAnonymousObject | 11.0.2 | Newtonsoft.Json 11.0.2 | 2.644 us | 0.0609 us | 0.0217 us |
| SerializeAnonymousObject |  9.0.1 |  Newtonsoft.Json 9.0.1 | 2.722 us | 0.3552 us | 0.1267 us |

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroNuGet

---