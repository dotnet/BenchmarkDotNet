---
uid: BenchmarkDotNet.Samples.IntroArrayParam
---

## Sample: IntroArrayParam

> [!WARNING]
> The cost of creating the arguments is not included in the benchmark.

So if you want to pass an array as an argument, we are going to allocate it before running the benchmark,
  and the benchmark will not include this operation.

### Source code

[!code-csharp[IntroArrayParam.cs](../../../samples/BenchmarkDotNet.Samples/IntroArrayParam.cs)]

### Output

```markdown
|        Method |      array | value |      Mean |     Error |    StdDev | Allocated |
|-------------- |----------- |------ |----------:|----------:|----------:|----------:|
|  ArrayIndexOf | Array[100] |     4 | 15.558 ns | 0.0638 ns | 0.0597 ns |       0 B |
| ManualIndexOf | Array[100] |     4 |  5.345 ns | 0.0668 ns | 0.0625 ns |       0 B |
|  ArrayIndexOf |   Array[3] |     4 | 14.334 ns | 0.1758 ns | 0.1558 ns |       0 B |
| ManualIndexOf |   Array[3] |     4 |  2.758 ns | 0.0905 ns | 0.1208 ns |       0 B |
|  ArrayIndexOf | Array[100] |   101 | 78.359 ns | 1.8853 ns | 2.0955 ns |       0 B |
| ManualIndexOf | Array[100] |   101 | 80.421 ns | 0.6391 ns | 0.5978 ns |       0 B |
```

### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroArrayParam

---