---
uid: BenchmarkDotNet.Samples.IntroCustomEnvironmentInfo
---

## Sample: IntroCustomEnvironmentInfo

You can add any useful information about environment to the benchmark report. Just mark one or several static methods that return `string` or `IEnumerable<string>` by the `[CustomEnvironmentInfo]` attribute.

### Source code

[!code-csharp[IntroCustomEnvironmentInfo.cs](../../../samples/BenchmarkDotNet.Samples/IntroCustomEnvironmentInfo.cs)]

### Links

* @docs.configs
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCustomEnvironmentInfo

---