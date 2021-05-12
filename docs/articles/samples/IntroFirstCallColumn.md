---
uid: BenchmarkDotNet.Samples.IntroFirstCallColumn
---

## Sample: IntroFirstCallColumn

If you want to have the execution time of the first call to be displayed in a dedicated column, please use `FirstCallColumn`.

### Usage

```cs
// attribute
[FirstCallColumn]
public class MyBenchmarkClass

// fluent style
class Program
{
    static void Main(string[] args) 
        => BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance.With(FirstCallColumn.Default));
}
```

### Source code

[!code-csharp[IntroColdStart.cs](../../../samples/BenchmarkDotNet.Samples/IntroFirstCallColumn.cs)]

### Output

```markdown
OverheadJitting  1: 1 op, 279920.00 ns, 279.9200 us/op
// First call
WorkloadJitting  1: 1 op, 1000605392.00 ns, 1.0006 s/op

WorkloadWarmup   1: 1 op, 10076266.00 ns, 10.0763 ms/op
WorkloadWarmup   2: 1 op, 10097996.00 ns, 10.0980 ms/op
WorkloadWarmup   3: 1 op, 10092053.00 ns, 10.0921 ms/op
WorkloadWarmup   4: 1 op, 10051001.00 ns, 10.0510 ms/op
WorkloadWarmup   5: 1 op, 10102372.00 ns, 10.1024 ms/op
WorkloadWarmup   6: 1 op, 10094080.00 ns, 10.0941 ms/op

// BeforeActualRun
WorkloadActual   1: 1 op, 10077178.00 ns, 10.0772 ms/op
```

```markdown
| Method | FirstCall |     Mean |    Error |   StdDev |
|------- |----------:|---------:|---------:|---------:|
|    Foo |     1.0 s | 10.10 ms | 0.023 ms | 0.022 ms |
```

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroFirstCallColumn

---