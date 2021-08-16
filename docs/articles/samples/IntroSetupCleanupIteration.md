---
uid: BenchmarkDotNet.Samples.IntroSetupCleanupIteration
---

## Sample: IntroSetupCleanupIteration

A method which is marked by the [`[IterationSetup]`](xref:BenchmarkDotNet.Attributes.IterationSetupAttribute)
  attribute will be executed exactly once *before each benchmark invocation*, forcing `UnrollFactor=1` and `InvocationCount=1` (we have changed that in 0.11.0).
It's not recommended to use this attribute in microbenchmarks because it can spoil the results.
However, if you are writing a macrobenchmark (e.g. a benchmark which takes at least 100ms) and
  you want to prepare some data before each invocation,
  [`[IterationSetup]`](xref:BenchmarkDotNet.Attributes.IterationSetupAttribute) can be useful.

A method which is marked by the [`[IterationCleanup]`](xref:BenchmarkDotNet.Attributes.IterationCleanupAttribute)
  attribute will be executed exactly once *after each invocation*.
This attribute has the same set of constraint with `[IterationSetup]`: it's not recommended to use
  [`[IterationCleanup]`](xref:BenchmarkDotNet.Attributes.IterationCleanupAttribute) in microbenchmarks.

### Source code

[!code-csharp[IntroSetupCleanupIteration.cs](../../../samples/BenchmarkDotNet.Samples/IntroSetupCleanupIteration.cs)]

### The order of method calls

```cs
// GlobalSetup

// IterationSetup (1)    // IterationSetup Jitting
// IterationCleanup (1)  // IterationCleanup Jitting

// IterationSetup (2)    // MainWarmup1
// Benchmark             // MainWarmup1
// IterationCleanup (2)  // MainWarmup1

// IterationSetup (3)    // MainWarmup2
// Benchmark             // MainWarmup2
// IterationCleanup (3)  // MainWarmup2

// IterationSetup (4)    // MainTarget1
// Benchmark             // MainTarget1
// IterationCleanup (4)  // MainTarget1

// IterationSetup (5)    // MainTarget2
// Benchmark             // MainTarget2
// IterationCleanup (5)  // MainTarget2

// IterationSetup (6)    // MainTarget3
// Benchmark             // MainTarget3
// IterationCleanup (6)  // MainTarget3

// GlobalCleanup
```

### Links

* @docs.setup-and-cleanup
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroSetupCleanupIteration

---
