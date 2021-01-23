---
uid: BenchmarkDotNet.Samples.IntroArgumentsPriority
---

## Sample: IntroArgumentsPriority

Like Params also Argument columns can be sorted in the table result through their `Priority`. The priority should be defined only once for multiple Arguments and will keep their inner order as they are defined in the method.

### Source code

[!code-csharp[IntroArgumentsPriority.cs](../../../samples/BenchmarkDotNet.Samples/IntroArgumentsPriority.cs)]

### Output

```markdown
|        Method |  b |   A | c | d |     Mean |   Error |  StdDev |
|-------------- |--- |---- |-- |-- |---------:|--------:|--------:|
| ManyArguments |  ? | 100 | 1 | 2 | 103.4 ms | 0.09 ms | 0.08 ms |
|     Benchmark |  5 | 100 | ? | ? | 105.5 ms | 0.21 ms | 0.19 ms |
|     Benchmark | 10 | 100 | ? | ? | 110.5 ms | 0.14 ms | 0.14 ms |
|     Benchmark | 20 | 100 | ? | ? | 120.4 ms | 0.16 ms | 0.15 ms |
```

### Links

* Priority BaseClass [`PriorityAttribute.cs`](xref:BenchmarkDotNet.Attributes.PriorityAttribute)
* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroArgumentsPriority

---