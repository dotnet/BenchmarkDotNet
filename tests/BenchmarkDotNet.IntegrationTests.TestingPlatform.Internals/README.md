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
  of the subset that was asked for would cost the machine the next hour. A run over an assembly that declares no
  benchmarks has nothing to carry that report, so the refusal goes to the output device instead of the run passing
  for a green, zero-test success.

The second thing out of a test host's reach is an application ending while a request is still in flight — the client
sending `exit`, or an IDE cancelling. This ends one three times over, once per state a request's parameter values can
be in:

* **enumerated and handed to nobody**, which `ParameterValueLifetime` must dispose on its way out, since nothing else
  can reach them and the finalizer is the dotnet/BenchmarkDotNet#1383 hang;
* **handed to BenchmarkDotNet**, which it must leave alone — they are disposed by the run stage's own `finally`, and
  taking them here would pull them out from under a run that is using or about to use them. Ownership runs from the
  hand-off rather than from the start of the run stage, because validation and the whole build stage sit in between;
* **handed over and taken back**, which happens when a critical validation error ends the run before the run stage, so
  BenchmarkDotNet disposed nothing and they are the request's again. Left undisposed they are the same hang as the
  first case, reached without anything throwing.

`TestingPlatformAdapterTests` in `BenchmarkDotNet.IntegrationTests` runs it and asserts on the report.
