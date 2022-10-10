---
uid: BenchmarkDotNet.Samples.IntroParamsPriority
---

## Sample: IntroParamsPriority

In order to sort columns of parameters in the results table you can use the Property `Priority` inside the params attribute. The priority range is `[Int32.MinValue;Int32.MaxValue]`, lower priorities will appear earlier in the column order. The default priority is set to `0`.

### Source code

[!code-csharp[IntroParamsPriority.cs](../../../samples/BenchmarkDotNet.Samples/IntroParamsPriority.cs)]

### Output

```markdown
|    Method |  B |   A |     Mean |   Error |  StdDev |
|---------- |--- |---- |---------:|--------:|--------:|
| Benchmark | 10 | 100 | 115.4 ms | 0.12 ms | 0.11 ms |
```

### Links

* Priority BaseClass [`PriorityAttribute.cs`](xref:BenchmarkDotNet.Attributes.PriorityAttribute)
* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroParamsPriority

---
