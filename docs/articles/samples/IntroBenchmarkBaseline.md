---
uid: BenchmarkDotNet.Samples.IntroBenchmarkBaseline
---

## Sample: IntroBenchmarkBaseline

You can mark a method as a baseline with the help of `[Benchmark(Baseline = true)]`.

### Source code

[!code-csharp[IntroBenchmarkBaseline.cs](../../../samples/BenchmarkDotNet.Samples/IntroBenchmarkBaseline.cs)]

### Output

As a result, you will have additional `Scaled` column in the summary table:

```markdown
|  Method |      Mean |     Error |    StdDev | Scaled |
|-------- |----------:|----------:|----------:|-------:|
|  Time50 |  50.46 ms | 0.0779 ms | 0.0729 ms |   0.50 |
| Time100 | 100.39 ms | 0.0762 ms | 0.0713 ms |   1.00 |
| Time150 | 150.48 ms | 0.0986 ms | 0.0922 ms |   1.50 |
```

### Links

* @docs.baselines
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroBenchmarkBaseline

---