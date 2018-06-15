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
   Method  |      Median |    StdDev |   A |  B
---------- |------------ |---------- |---- |---
 Benchmark | 115.3325 ms | 0.0242 ms | 100 | 10
 Benchmark | 125.3282 ms | 0.0245 ms | 100 | 20
 Benchmark | 215.3024 ms | 0.0375 ms | 200 | 10
 Benchmark | 225.2710 ms | 0.0434 ms | 200 | 20
```

### Remarks

**A remark about IParam.**

You don't need to use `IParam` anymore since `0.11.0`.
Just use complex types as you wish and override `ToString` method to change the display names used in the results.


### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroParamsSource

---