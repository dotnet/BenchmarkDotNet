---
uid: BenchmarkDotNet.Samples.IntroParams
---

## Sample: IntroParams

You can mark one or several fields or properties in your class by
  the [`[Params]`](xref:BenchmarkDotNet.Attributes.ParamsAttribute) attribute.
In this attribute, you can specify set of values.
Every value must be a compile-time constant.
As a result, you will get results for each combination of params values.

### Source code

[!code-csharp[IntroParams.cs](../../../samples/BenchmarkDotNet.Samples/IntroParams.cs)]

### Output

```markdown
|    Method |   A |  B |     Mean |   Error |  StdDev |
|---------- |---- |--- |---------:|--------:|--------:|
| Benchmark | 100 | 10 | 115.3 ms | 0.13 ms | 0.12 ms |
| Benchmark | 100 | 20 | 125.4 ms | 0.14 ms | 0.12 ms |
| Benchmark | 200 | 10 | 215.5 ms | 0.19 ms | 0.18 ms |
| Benchmark | 200 | 20 | 225.4 ms | 0.17 ms | 0.16 ms |
```

### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroParams

---