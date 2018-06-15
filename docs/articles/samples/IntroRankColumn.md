---
uid: BenchmarkDotNet.Samples.IntroRankColumn
---

## Sample: IntroRankColumn


### Source code

[!code-csharp[IntroRankColumn.cs](../../../samples/BenchmarkDotNet.Samples/IntroRankColumn.cs)]

### Output

```markdown
 Method | Factor |     Mean |    Error |    StdDev | Rank | Rank | Rank |
------- |------- |---------:|---------:|----------:|-----:|-----:|-----:|
    Foo |      1 | 100.8 ms | 2.250 ms | 0.1272 ms |    1 |    I |    * |
    Foo |      2 | 200.8 ms | 4.674 ms | 0.2641 ms |    2 |   II |   ** |
    Bar |      1 | 200.9 ms | 2.012 ms | 0.1137 ms |    2 |   II |   ** |
    Bar |      2 | 400.7 ms | 4.509 ms | 0.2548 ms |    3 |  III |  *** |
```

### Links

* @docs.statistics
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroRankColumn

---