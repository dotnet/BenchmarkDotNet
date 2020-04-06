---
uid: BenchmarkDotNet.Samples.IntroEventPipeProfiler
---

## Sample: EventPipeProfiler

The `EventPipeProfiler` can be enabled using the `[EventPipeProfiler(...)]` attribute. This attribute takes the following profiles:
 - `CpuSampling` - Useful for tracking CPU usage and general .NET runtime information. This is the default option.
 - `GcVerbose` - Tracks GC collections and samples object allocations.
 - `GcCollect` - Tracks GC collections only at very low overhead.
 - `Jit` - Logging when Just in time (JIT) compilation occurs. Logging of the internal workings of the Just In Time compiler. This is fairly verbose. It details decisions about interesting optimization (like inlining and tail call)

### Source code

[!code-csharp[EventPipeProfiler.cs](../../../samples/BenchmarkDotNet.Samples/IntroEventPipeProfiler.cs)]

### Output

The output should contain information about the exported trace file which can be analyzed using [SpeedScope](https://www.speedscope.app/).

```markdown
// * Diagnostic Output - EventPipeProfiler *
Exported 1 trace file(s). Example:
C:\Work\BenchmarkDotNet\samples\BenchmarkDotNet.Samples\BenchmarkDotNet.Artifacts\BenchmarkDotNet.Samples.IntroEventPipeProfiler.Sleep-20200406-090113.speedscope.json
```
