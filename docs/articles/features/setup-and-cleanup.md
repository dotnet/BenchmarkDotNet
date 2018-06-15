---
uid: docs.setup-and-cleanup
name: Setup And Cleanup
---

# Setup And Cleanup

Sometimes we want to write some logic which should be executed *before* or *after* a benchmark, but we don't want to measure it.
For this purpose, BenchmarkDotNet provides a set of attributes:
  [`[GlobalSetup]`](xref:BenchmarkDotNet.Attributes.GlobalSetupAttribute),
  [`[GlobalCleanup]`](xref:BenchmarkDotNet.Attributes.GlobalCleanupAttribute),
  [`[IterationSetup]`](xref:BenchmarkDotNet.Attributes.IterationSetupAttribute),
  [`[IterationCleanup]`](xref:BenchmarkDotNet.Attributes.IterationCleanupAttribute).

---

[!include[IntroSetupCleanupGlobal](../samples/IntroSetupCleanupGlobal.md)]

[!include[IntroSetupCleanupIteration](../samples/IntroSetupCleanupIteration.md)]

[!include[IntroSetupCleanupTarget](../samples/IntroSetupCleanupTarget.md)]