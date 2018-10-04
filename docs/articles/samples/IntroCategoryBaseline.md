---
uid: BenchmarkDotNet.Samples.IntroCategoryBaseline
---

## Sample: IntroCategoryBaseline

The only way to have several baselines in the same class is to separate them by categories
  and mark the class with `[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]`.

### Source code

[!code-csharp[IntroCategoryBaseline.cs](../../../samples/BenchmarkDotNet.Samples/IntroCategoryBaseline.cs)]

### Output

```markdown
|  Method | Categories |      Mean |     Error |    StdDev | Ratio |
|-------- |----------- |----------:|----------:|----------:|------:|
|  Time50 |       Fast |  50.46 ms | 0.0745 ms | 0.0697 ms |  1.00 |
| Time100 |       Fast | 100.47 ms | 0.0955 ms | 0.0893 ms |  1.99 |
|         |            |           |           |           |       |
| Time550 |       Slow | 550.48 ms | 0.0525 ms | 0.0492 ms |  1.00 |
| Time600 |       Slow | 600.45 ms | 0.0396 ms | 0.0331 ms |  1.09 |
```

### Links

* @docs.baselines
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCategoryBaseline

---