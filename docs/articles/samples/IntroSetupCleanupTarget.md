---
uid: BenchmarkDotNet.Samples.IntroSetupCleanupTarget
---

## Sample: IntroSetupCleanupTarget

Sometimes it's useful to run setup or cleanups for specific benchmarks.
All four setup and cleanup attributes have a Target property that allow
  the setup/cleanup method to be run for one or more specific benchmark methods.

### Source code

[!code-csharp[IntroSetupCleanupTarget.cs](../../../samples/BenchmarkDotNet.Samples/IntroSetupCleanupTarget.cs)]

### The order of method calls

```cs
// GlobalSetup A

// Benchmark A

// GlobalSetup B

// Benchmark B

// GlobalSetup B

// Benchmark C

// Benchmark D
```

### Links

* @docs.setup-and-cleanup
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroSetupCleanupTarget

---
