---
uid: BenchmarkDotNet.Samples.IntroJobBaseline
---

## Sample: IntroJobBaseline

If you want to compare several runtime configuration,
  you can mark one of your jobs with `baseline = true`.

### Source code

[!code-csharp[IntroJobBaseline.cs](../../../samples/BenchmarkDotNet.Samples/IntroJobBaseline.cs)]

### Output

```ini
BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.192)
Processor=Intel Core i7-6700HQ CPU 2.60GHz (Skylake), ProcessorCount=8
Frequency=2531249 Hz, Resolution=395.0619 ns, Timer=TSC
.NET Core SDK=2.0.3
  [Host]     : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  Job-MXFYPZ : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2600.0
  Core       : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  Mono       : Mono 5.4.0 (Visual Studio), 64bit 
```

```markdown
    Method | Runtime |     Mean |     Error |    StdDev | Ratio | RatioSD |
---------- |-------- |---------:|----------:|----------:|------:|--------:|
 SplitJoin |     Clr | 19.42 us | 0.2447 us | 0.1910 us |  1.00 |    0.00 |
 SplitJoin |    Core | 13.00 us | 0.2183 us | 0.1935 us |  0.67 |    0.01 |
 SplitJoin |    Mono | 39.14 us | 0.7763 us | 1.3596 us |  2.02 |    0.07 |
```

### Links

* @docs.baselines
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroJobBaseline

---