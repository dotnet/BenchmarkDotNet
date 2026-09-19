# BenchmarkDotNet.IntegrationTests.TestingPlatform.Failures

Every benchmark in this Microsoft.Testing.Platform application is expected to be reported as failed, so a plain
`dotnet test` over it fails by design. It exists for the paths of `BenchmarkDotNet.TestAdapter` that only a broken
benchmark reaches: the uid collision report and the mapping of a build failure onto failed tests.

`TestingPlatformAdapterTests` in `BenchmarkDotNet.IntegrationTests` drives it, one probe at a time, and asserts on
what the platform reports. The benchmarks that are expected to pass live in
`BenchmarkDotNet.IntegrationTests.TestingPlatform` instead.
