---
uid: BenchmarkDotNet.Samples.IntroComparableComplexParam
---

## Sample: IntroComparableComplexParam

You can implement `IComparable` (the non generic version) on your complex parameter class if you want custom ordering behavior for your parameter.

One use case for this is having a parameter class that overrides `ToString()`, but also providing a custom ordering behavior that isn't the alphabetical order of the result of `ToString()`.

### Source code

[!code-csharp[IntroComparableComplexParam.cs](../../../samples/BenchmarkDotNet.Samples/IntroComparableComplexParam.cs)]

### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroComparableComplexParam

---