---
uid: BenchmarkDotNet.Samples.IntroEnvVars
---

## Sample: IntroEnvVars

You can configure custom environment variables for the process that is running your benchmarks.
One reason for doing this might be checking out how different
 [compilation](https://learn.microsoft.com/dotnet/core/runtime-config/compilation),
 [threading](https://learn.microsoft.com/dotnet/core/runtime-config/threading),
 [garbage collector](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector)
 settings affect the performance of .NET Core.

### Source code

[!code-csharp[IntroEnvVars.cs](../../../samples/BenchmarkDotNet.Samples/IntroEnvVars.cs)]

### Links

* @docs.customizing-runtime
* @docs.configs
* @docs.jobs
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroEnvVars

---