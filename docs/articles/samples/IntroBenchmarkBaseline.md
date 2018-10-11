---
uid: BenchmarkDotNet.Samples.IntroBenchmarkBaseline
---

## Sample: IntroBenchmarkBaseline

You can mark a method as a baseline with the help of `[Benchmark(Baseline = true)]`.

### Source code

[!code-csharp[IntroBenchmarkBaseline.cs](../../../samples/BenchmarkDotNet.Samples/IntroBenchmarkBaseline.cs)]

### Output

As a result, you will have additional `Ratio` column in the summary table:

```markdown
|  Method |      Mean |     Error |    StdDev | Ratio |
|-------- |----------:|----------:|----------:|------:|
|  Time50 |  50.46 ms | 0.0779 ms | 0.0729 ms |  0.50 |
| Time100 | 100.39 ms | 0.0762 ms | 0.0713 ms |  1.00 |
| Time150 | 150.48 ms | 0.0986 ms | 0.0922 ms |  1.50 |
```

This column contains the mean value of the ratio distribution.

For example, in the case of `Time50`, we divide
  the first measurement of `Time50` into the first measurement of `Time100` (it's the baseline),
  the second measurement of `Time50` into the second measurement of `Time100`,
  and so on.
Next, we calculate the mean of all these values and display it in the `Ratio` column.
For `Time50`, we have 0.50.

The `Ratio` column was formerly known as `Scaled`.
The old title was a source of misunderstanding and confusion because
  many developers interpreted it as the ratio of means (e.g., `50.46`/`100.39` for `Time50`).
The ratio of distribution means and the mean of the ratio distribution are pretty close to each other in most cases,
  but they are not equal.

In @BenchmarkDotNet.Samples.IntroRatioStdDev, you can find an example of how this value can be spoiled by outliers.

### Links

* @docs.baselines
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroBenchmarkBaseline

---