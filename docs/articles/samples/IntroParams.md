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
   Method  |      Median |    StdDev |   A |  B
---------- |------------ |---------- |---- |---
 Benchmark | 115.3325 ms | 0.0242 ms | 100 | 10
 Benchmark | 125.3282 ms | 0.0245 ms | 100 | 20
 Benchmark | 215.3024 ms | 0.0375 ms | 200 | 10
 Benchmark | 225.2710 ms | 0.0434 ms | 200 | 20
```

### See also

* @docs.parameterization