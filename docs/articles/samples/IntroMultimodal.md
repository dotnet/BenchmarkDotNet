---
uid: BenchmarkDotNet.Samples.IntroMultimodal
---

## Sample: IntroMultimodal


### Source code

[!code-csharp[IntroMultimodal.cs](../../../samples/BenchmarkDotNet.Samples/IntroMultimodal.cs)]

### Output

```markdown
      Method |     Mean |      Error |      StdDev |   Median | MValue |
------------ |---------:|-----------:|------------:|---------:|-------:|
    Unimodal | 100.5 ms |  0.0713 ms |   0.0667 ms | 100.5 ms |  2.000 |
     Bimodal | 144.5 ms | 16.9165 ms |  49.8787 ms | 100.6 ms |  3.571 |
    Trimodal | 182.5 ms | 27.4285 ms |  80.8734 ms | 200.5 ms |  4.651 |
 Quadrimodal | 226.6 ms | 37.2269 ms | 109.7644 ms | 200.7 ms |  5.882 |

// * Warnings *
MultimodalDistribution
  IntroMultimodal.Bimodal: MainJob     -> It seems that the distribution is bimodal (mValue = 3.57)
  IntroMultimodal.Trimodal: MainJob    -> It seems that the distribution is multimodal (mValue = 4.65)
  IntroMultimodal.Quadrimodal: MainJob -> It seems that the distribution is multimodal (mValue = 5.88)
```

### Links

* @docs.statistics
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroMultimodal

---