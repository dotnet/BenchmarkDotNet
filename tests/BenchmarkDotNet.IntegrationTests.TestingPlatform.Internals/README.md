# BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals

Drives the adapter's Microsoft.Testing.Platform types directly, to reach what a real test host cannot.

The first of those is `BenchmarkTestFramework` with a test execution filter that `BenchmarkDotNet.TestAdapter` does not recognise,
which is the one thing the other probe applications cannot do: Microsoft.Testing.Platform 2.3.3 ships `NopFilter`,
`TestNodeUidListFilter` and `TreeNodeFilter`, the adapter handles all three, and `ITestExecutionFilterFactory` — the
extension point a consumer would register another one through — is internal to the platform. So the branch that
handles an unknown filter is unreachable from a real test host, and a regression in it would be silent.

`ITestExecutionFilter` itself is public, so this application implements one, hands it to the framework through a
discovery request and a run request, and prints what the framework did with each. It is not a Microsoft.Testing.Platform
application: it has its own entry point, and stands in for the platform around the framework.

The two requests are deliberately treated differently, which is what this pins:

* **discovery** lists every benchmark and warns on the output device — a wrong list costs nothing to correct;
* **a run** refuses, reporting every benchmark it could have selected as failed — running the whole assembly instead
  of the subset that was asked for would cost the machine the next hour.

It also ends an application while a request is still in flight - the client sending `exit`, or an IDE cancelling -
which no driven test host can be made to do on cue either. The values that request had enumerated are reachable from
nothing else, so `ParameterValueLifetime` has to dispose them on its way out; left to the finalizer they are the
dotnet/BenchmarkDotNet#1383 hang.

`TestingPlatformAdapterTests` in `BenchmarkDotNet.IntegrationTests` runs it and asserts on the report.
