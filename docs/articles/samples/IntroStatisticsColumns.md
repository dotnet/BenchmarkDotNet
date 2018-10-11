---
uid: BenchmarkDotNet.Samples.IntroStatisticsColumns
---

## Sample: IntroStatisticsColumns


### Source code

[!code-csharp[IntroStatisticsColumns.cs](../../../samples/BenchmarkDotNet.Samples/IntroStatisticsColumns.cs)]

### Output

| Method |     Mean |     Error |    StdDev | Skewness | Kurtosis | Ratio | RatioSD |
|------- |---------:|----------:|----------:|---------:|---------:|------:|--------:|
|   Md5A | 15.91 us | 0.0807 us | 0.1209 us |   0.4067 |    1.646 |  1.00 |    0.00 |
|   Md5B | 15.89 us | 0.0709 us | 0.1062 us |   0.5893 |    2.141 |  1.00 |    0.01 |
| Sha256 | 36.62 us | 0.6390 us | 0.9564 us |   1.1363 |    4.014 |  2.30 |    0.06 |


### Links

* @docs.statistics
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroStatisticsColumns

---