---
uid: BenchmarkDotNet.Samples.IntroEventPipeProfilerAdvanced
---

## Sample: EventPipeProfilerAdvanced

The most advanced and powerful way to use `EventPipeProfiler` is a custom configuration. As you can see the below configuration adds `EventPipeProfiler` that constructor can take the profile and/or a list of providers. 
Both `EventPipeProfiler` and `dotnet trace` use the `Microsoft.Diagnostics.NETCore.Client` package internally. So before you start using the custom configuration of this profiler, it is worth reading the documentation [here](https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md) and [here](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-collect) where you can fine more information about how to configure provider list.

### Source code

[!code-csharp[EventPipeProfilerAdvanced.cs](../../../samples/BenchmarkDotNet.Samples/IntroEventPipeProfilerAdvanced.cs)]