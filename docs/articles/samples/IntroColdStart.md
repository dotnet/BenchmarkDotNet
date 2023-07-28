---
#cspell:ignore runstrategy
uid: BenchmarkDotNet.Samples.IntroColdStart
---

## Sample: IntroColdStart

If you want to measure cold start (without the pilot and warmup stage), the `ColdStart` strategy is your choice.

### Usage

```cs
[SimpleJob(RunStrategy.ColdStart, launchCount:50)]
public class MyBenchmarkClass
```

### Source code

[!code-csharp[IntroColdStart.cs](../../../samples/BenchmarkDotNet.Samples/IntroColdStart.cs)]

### Output

```markdown
Result       1: 1 op, 1002034900.00 ns, 1.0020 s/op
Result       2: 1 op, 10219700.00 ns, 10.2197 ms/op
Result       3: 1 op, 10406200.00 ns, 10.4062 ms/op
Result       4: 1 op, 10473900.00 ns, 10.4739 ms/op
Result       5: 1 op, 10449400.00 ns, 10.4494 ms/op
```

```markdown
 Method |     Mean |      Error |   StdDev |      Min |        Max |   Median |
------- |---------:|-----------:|---------:|---------:|-----------:|---------:|
    Foo | 208.7 ms | 1,707.4 ms | 443.5 ms | 10.22 ms | 1,002.0 ms | 10.45 ms |
```

### Links

* @docs.runstrategy
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroColdStart

---
