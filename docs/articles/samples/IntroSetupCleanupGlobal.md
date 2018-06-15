---
uid: BenchmarkDotNet.Samples.IntroSetupCleanupGlobal
---

## Sample: IntroSetupCleanupGlobal

A method which is marked by the [`[GlobalSetup]`](xref:BenchmarkDotNet.Attributes.GlobalSetupAttribute)
  attribute will be executed only once per a benchmarked method
  after initialization of benchmark parameters and before all the benchmark method invocations.
A method which is marked by the [`[GlobalCleanup]`](xref:BenchmarkDotNet.Attributes.GlobalCleanupAttribute)
  attribute will be executed only once per a benchmarked method
  after all the benchmark method invocations.
If you are using some unmanaged resources (e.g., which were created in the `GlobalSetup` method),
  they can be disposed in the `GlobalCleanup` method.

### Source code

[!code-csharp[IntroSetupCleanupGlobal.cs](../../../samples/BenchmarkDotNet.Samples/IntroSetupCleanupGlobal.cs)]

### Links

* @docs.setup-and-cleanup
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroSetupCleanupGlobal

---