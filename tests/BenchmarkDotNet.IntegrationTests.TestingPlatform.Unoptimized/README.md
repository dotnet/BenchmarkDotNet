# BenchmarkDotNet.IntegrationTests.TestingPlatform.Unoptimized

A Microsoft.Testing.Platform application built with `<Optimize>false</Optimize>`, which is the one thing that makes it
different from `BenchmarkDotNet.IntegrationTests.TestingPlatform`.

`BenchmarkEnumerator` hides the benchmarks that would run out of process when the assembly is not optimized, so that
they can be debugged from a test runner. Those benchmarks are enumerated first and BenchmarkDotNet never sees them
afterwards, so the parameter values they own are the adapter's to dispose - a value with a locking finalizer hangs the
runtime otherwise, see dotnet/BenchmarkDotNet#1383. Every other project in this repository is optimized, deliberately,
which leaves that path unreachable in a Release CI run.

`TestingPlatformAdapterTests` in `BenchmarkDotNet.IntegrationTests` drives it.
