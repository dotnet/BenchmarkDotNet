---
uid: BenchmarkDotNet.Samples.IntroStatisticalTesting
---

## Sample: IntroStatisticalTesting

### Source code

[!code-csharp[IntroStatisticalTesting.cs](../../../samples/BenchmarkDotNet.Samples/IntroStatisticalTesting.cs)]

### Output
```markdown
|   Method |      Mean |     Error |    StdDev | Ratio |   Welch(1us)/p-values |    Welch(3%)/p-values | MannWhitney(1us)/p-values | MannWhitney(3%)/p-values |
|--------- |----------:|----------:|----------:|------:|---------------------- |---------------------- |-------------------------- |------------------------- |
|  Sleep50 |  53.13 ms | 0.5901 ms | 0.1532 ms |  0.51 | Faster: 1.0000/0.0000 | Faster: 1.0000/0.0000 |     Faster: 1.0000/0.0040 |    Faster: 1.0000/0.0040 |
|  Sleep97 | 100.07 ms | 0.9093 ms | 0.2361 ms |  0.97 | Faster: 1.0000/0.0000 |   Same: 1.0000/0.1290 |     Faster: 1.0000/0.0040 |      Same: 1.0000/0.1111 |
|  Sleep99 | 102.23 ms | 2.4462 ms | 0.6353 ms |  0.99 | Faster: 0.9928/0.0072 |   Same: 1.0000/0.9994 |     Faster: 0.9960/0.0079 |      Same: 1.0000/1.0000 |
| Sleep100 | 103.34 ms | 0.8180 ms | 0.2124 ms |  1.00 |   Base: 0.5029/0.5029 |   Base: 1.0000/1.0000 |       Base: 0.7262/0.7262 |      Base: 1.0000/1.0000 |
| Sleep101 | 103.73 ms | 2.1591 ms | 0.5607 ms |  1.00 |   Same: 0.1041/0.8969 |   Same: 0.9999/1.0000 |       Same: 0.1111/0.9246 |      Same: 1.0000/1.0000 |
| Sleep103 | 106.21 ms | 1.2511 ms | 0.3249 ms |  1.03 | Slower: 0.0000/1.0000 |   Same: 0.9447/1.0000 |     Slower: 0.0040/1.0000 |      Same: 0.9246/1.0000 |
| Sleep150 | 153.16 ms | 3.4929 ms | 0.9071 ms |  1.48 | Slower: 0.0000/1.0000 | Slower: 0.0000/1.0000 |     Slower: 0.0040/1.0000 |    Slower: 0.0040/1.0000 |

// * Legends *
  Mean                      : Arithmetic mean of all measurements
  Error                     : Half of 99.9% confidence interval
  StdDev                    : Standard deviation of all measurements
  Ratio                     : Mean of the ratio distribution ([Current]/[Baseline])
  Welch(1us)/p-values       : Welch-based TOST equivalence test with 1 us threshold. Format: 'Result: p-value(Slower)|p-value(Faster)'
  Welch(3%)/p-values        : Welch-based TOST equivalence test with 3% threshold. Format: 'Result: p-value(Slower)|p-value(Faster)'
  MannWhitney(1us)/p-values : MannWhitney-based TOST equivalence test with 1 us threshold. Format: 'Result: p-value(Slower)|p-value(Faster)'
  MannWhitney(3%)/p-values  : MannWhitney-based TOST equivalence test with 3% threshold. Format: 'Result: p-value(Slower)|p-value(Faster)'
  1 ms                      : 1 Millisecond (0.001 sec)
```

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroStatisticalTesting

---