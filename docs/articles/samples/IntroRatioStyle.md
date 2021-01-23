---
uid: BenchmarkDotNet.Samples.IntroRatioStyle
---

## Sample: IntroRatioStyle

Using `RatioStyle`, we can override the style of the "Ratio" column in `SummaryStyle`.
Here are the possible values:

* `Value`: default value that shows the ration value between the current benchmark and the baseline benchmark (e.g., `0.15` or `1.15`)
* `Percentage`: express the ration in percentage (e.g., `-85%` or `+15%`)
* `Trend`: shows how much the current benchmark is faster or slower than the base benchmark (e.g., `6.63x faster` or `1.15x slower`)

### Source code

[!code-csharp[IntroRatioStyle.cs](../../../samples/BenchmarkDotNet.Samples/IntroRatioStyle.cs)]

### Output

With the given `RatioStyle.Trend`, we have the following table:

```markdown
|   Method |       Mean |   Error |  StdDev |        Ratio | RatioSD |
|--------- |-----------:|--------:|--------:|-------------:|--------:|
| Baseline | 1,000.6 ms | 2.48 ms | 0.14 ms |     baseline |         |
|      Bar |   150.9 ms | 1.30 ms | 0.07 ms | 6.63x faster |   0.00x |
|      Foo | 1,150.4 ms | 5.17 ms | 0.28 ms | 1.15x slower |   0.00x |
```

With the default `RatioStyle.Value`, we get the following table:

```markdown
|   Method |       Mean |   Error |  StdDev | Ratio |
|--------- |-----------:|--------:|--------:|------:|
| Baseline | 1,000.3 ms | 2.71 ms | 0.15 ms |  1.00 |
|      Bar |   150.6 ms | 1.67 ms | 0.09 ms |  0.15 |
|      Foo | 1,150.6 ms | 7.41 ms | 0.41 ms |  1.15 |
```

If we use `RatioStyle.Percentage`, we get the following table:

```markdown
|   Method |       Mean |   Error |  StdDev |    Ratio | RatioSD |
|--------- |-----------:|--------:|--------:|---------:|--------:|
| Baseline | 1,000.3 ms | 4.69 ms | 0.26 ms | baseline |         |
|      Bar |   150.7 ms | 1.42 ms | 0.08 ms |     -85% |    0.1% |
|      Foo | 1,150.3 ms | 6.13 ms | 0.34 ms |     +15% |    0.0% |
```

### Links

* @docs.baselines
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroRatioStyle

---
