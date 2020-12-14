---
uid: BenchmarkDotNet.Samples.IntroParamsSource
---

## Sample: IntroParamsSource

In case you want to use a lot of values, you should use
  [`[ParamsSource]`](xref:BenchmarkDotNet.Attributes.ParamsSourceAttribute)
You can mark one or several fields or properties in your class by the
  [`[Params]`](xref:BenchmarkDotNet.Attributes.ParamsAttribute) attribute.
In this attribute, you have to specify the name of public method/property which is going to provide the values
  (something that implements `IEnumerable`).
The source must be within benchmarked type!

### Source code

[!code-csharp[IntroParamsSource.cs](../../../samples/BenchmarkDotNet.Samples/IntroParamsSource.cs)]

### Output

```markdown
|    Method |  B |   A |     Mean |   Error |  StdDev |
|---------- |--- |---- |---------:|--------:|--------:|
| Benchmark | 10 | 100 | 115.5 ms | 0.17 ms | 0.16 ms |
| Benchmark | 10 | 200 | 215.6 ms | 0.15 ms | 0.14 ms |
| Benchmark | 20 | 100 | 125.5 ms | 0.19 ms | 0.18 ms |
| Benchmark | 20 | 200 | 225.5 ms | 0.23 ms | 0.22 ms |
```

### Remarks

**A remark about IParam.**

You don't need to use `IParam` anymore since `0.11.0`.
Just use complex types as you wish and override `ToString` method to change the display names used in the results.


### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroParamsSource

---