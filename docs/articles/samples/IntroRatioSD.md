---
uid: BenchmarkDotNet.Samples.IntroRatioSD
---

## Sample: IntroRatioSD

The ratio of two benchmarks is not a single number, it's a distribution.
In most simple cases, the range of the ratio distribution is narrow,
  and BenchmarkDotNet displays a single column `Ratio` with the mean value.
However, it also adds the `RatioSD` column (the standard deviation of the ratio distribution)
  in complex situations.
In the below example, the baseline benchmark is spoiled by a single outlier

### Source code

[!code-csharp[IntroRatioSD.cs](../../../samples/BenchmarkDotNet.Samples/IntroRatioSD.cs)]

### Output

Here are statistics details for the baseline benchmark:

```ini
Mean = 600.6054 ms, StdErr = 500.0012 ms (83.25%); N = 10, StdDev = 1,581.1428 ms
Min = 100.2728 ms, Q1 = 100.3127 ms, Median = 100.4478 ms, Q3 = 100.5011 ms, Max = 5,100.6163 ms
IQR = 0.1884 ms, LowerFence = 100.0301 ms, UpperFence = 100.7837 ms
ConfidenceInterval = [-1,789.8568 ms; 2,991.0677 ms] (CI 99.9%), Margin = 2,390.4622 ms (398.01% of Mean)
Skewness = 2.28, Kurtosis = 6.57, MValue = 2
-------------------- Histogram --------------------
[-541.891 ms ;  743.427 ms) | @@@@@@@@@
[ 743.427 ms ; 2027.754 ms) | 
[2027.754 ms ; 3312.082 ms) | 
[3312.082 ms ; 4458.453 ms) | 
[4458.453 ms ; 5742.780 ms) | @
---------------------------------------------------
```

As you can see, a single outlier significantly affected the metrics.
Because of this, BenchmarkDotNet adds the `Median` and the `RatioSD` columns in the summary table:

```markdown
 Method |      Mean |         Error |        StdDev |    Median | Ratio | RatioSD |
------- |----------:|--------------:|--------------:|----------:|------:|--------:|
   Base | 600.61 ms | 2,390.4622 ms | 1,581.1428 ms | 100.45 ms |  1.00 |    0.00 |
   Slow | 200.50 ms |     0.4473 ms |     0.2959 ms | 200.42 ms |  1.80 |    0.62 |
   Fast |  50.54 ms |     0.3435 ms |     0.2272 ms |  50.48 ms |  0.45 |    0.16 |
```

Let's look at the `Base` and `Slow` benchmarks.
The `Mean` values are `600` and `200` milliseconds; the "Scaled Mean" value is 0.3.
The `Median` values are `100` and `200` milliseconds; the "Scaled Median" value is 2.
Both values are misleading.
BenchmarkDotNet evaluates the ratio distribution and displays the mean (1.80) and the standard deviation (0.62).

### Links

* @docs.baselines
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroRatioSD

---
