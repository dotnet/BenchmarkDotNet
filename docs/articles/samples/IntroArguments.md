---
uid: BenchmarkDotNet.Samples.IntroArguments
---

## Sample: IntroArguments

As an alternative to using [`[Params]`](xref:BenchmarkDotNet.Attributes.ParamsAttribute),
  you can specify arguments for your benchmarks.
There are several ways to do it (described below).


The [`[Arguments]`](xref:BenchmarkDotNet.Attributes.ArgumentsAttribute) allows you to provide a set of values.
Every value must be a compile-time constant (it's C# language limitation for attributes in general).
You can also combine
  [`[Arguments]`](xref:BenchmarkDotNet.Attributes.ArgumentsAttribute) with
  [`[Params]`](xref:BenchmarkDotNet.Attributes.ParamsAttribute).
As a result, you will get results for each combination of params values.

### Source code

[!code-csharp[IntroArguments.cs](../../../samples/BenchmarkDotNet.Samples/IntroArguments.cs)]

### Output

```markdown
|    Method | AddExtra5Miliseconds |   a |  b |     Mean |     Error |    StdDev |
|---------- |--------------------- |---- |--- |---------:|----------:|----------:|
| Benchmark |                False | 100 | 10 | 110.1 ms | 0.0056 ms | 0.0044 ms |
| Benchmark |                False | 100 | 20 | 120.1 ms | 0.0155 ms | 0.0138 ms |
| Benchmark |                False | 200 | 10 | 210.2 ms | 0.0187 ms | 0.0175 ms |
| Benchmark |                False | 200 | 20 | 220.3 ms | 0.1055 ms | 0.0986 ms |
| Benchmark |                 True | 100 | 10 | 115.3 ms | 0.1375 ms | 0.1286 ms |
| Benchmark |                 True | 100 | 20 | 125.3 ms | 0.1212 ms | 0.1134 ms |
| Benchmark |                 True | 200 | 10 | 215.4 ms | 0.0779 ms | 0.0691 ms |
| Benchmark |                 True | 200 | 20 | 225.4 ms | 0.0775 ms | 0.0725 ms |
```

### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroArguments

---