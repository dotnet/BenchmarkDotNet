---
uid: BenchmarkDotNet.Samples.IntroCustomEnvironmentInfo
---

## Sample: IntroCustomEnvironmentInfo

You can add any useful information about environment to the benchmark report. Just mark one or several static methods that return `string` or `IEnumerable<string>` by the [`[CustomEnvironmentInfo]`](xref:BenchmarkDotNet.Attributes.CustomEnvironmentInfoAttribute) attribute.

### Source code

[!code-csharp[IntroCustomEnvironmentInfo.cs](../../../samples/BenchmarkDotNet.Samples/IntroCustomEnvironmentInfo.cs)]

### Output

```
BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3419929 Hz, Resolution=292.4037 ns, Timer=TSC
.NET Core SDK=2.1.402
IsServerGC=False
args[0]=D:\BenchmarkDotNet\samples\BenchmarkDotNet.Samples\bin\Release\netcoreapp2.1\BenchmarkDotNet.Samples.dll
  [Host] : .NET Core 2.1.4 (CoreCLR 4.6.26814.03, CoreFX 4.6.26814.02), 64bit RyuJIT
  Dry    : .NET Core 2.1.4 (CoreCLR 4.6.26814.03, CoreFX 4.6.26814.02), 64bit RyuJIT
```

### Links

* @docs.configs
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCustomEnvironmentInfo

---