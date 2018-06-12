---
uid: BenchmarkDotNet.Samples.IntroCategories
---

## Sample: IntroCategories

Combined together with `[BenchmarkCategory]` attribute, you can group the benchmarks into categories and filter them by categories.

### Source code

[!code-csharp[IntroCategories.cs](../../../samples/BenchmarkDotNet.Samples/IntroCategories.cs)]

### Command line examples:
    
```
--category=A
--allCategories=A,B
--anyCategories=A,B
```

### See also

* @docs.filters
