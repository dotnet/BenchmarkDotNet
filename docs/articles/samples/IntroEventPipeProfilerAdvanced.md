---
uid: BenchmarkDotNet.Samples.IntroEventPipeProfilerAdvanced
---

## Sample: EventPipeProfilerAdvanced

The most advanced and powerful way to use `EventPipeProfiler` is a custom configuration. As you can see the below configuration adds `EventPipeProfiler` that constructor can take the profile and/or a list of providers. 
Both `EventPipeProfiler` and `dotnet trace` use the `Microsoft.Diagnostics.NETCore.Client` package internally. So before you start using the custom configuration of this profiler, it is worth reading the documentation [here](https://github.com/dotnet/diagnostics/blob/main/documentation/dotnet-trace-instructions.md) and [here](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-collect) where you can find more information about how to configure provider list.

### Source code

[!code-csharp[EventPipeProfilerAdvanced.cs](../../../samples/BenchmarkDotNet.Samples/IntroEventPipeProfilerAdvanced.cs)]

### Output

The output should contain information about the exported trace file which can be analyzed using [SpeedScope](https://www.speedscope.app/).

```markdown
// * Diagnostic Output - EventPipeProfiler *
Exported 1 trace file(s). Example:
C:\Work\BenchmarkDotNet\samples\BenchmarkDotNet.Samples\BenchmarkDotNet.Artifacts\BenchmarkDotNet.Samples.IntroEventPipeProfilerAdvanced.RentAndReturn_Shared-20200406-090136.speedscope.json
```
