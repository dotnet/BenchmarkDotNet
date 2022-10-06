---
uid: docs.event-pipe-profiler
name: EventPipeProfiler
---

# EventPipeProfiler

`EventPipeProfiler` is a cross-platform profiler that allows profile .NET code on every platform - Windows, Linux, macOS. Collected data are exported to trace files (`.speedscope.json` and `.nettrace`) which can be analyzed using [SpeedScope](https://www.speedscope.app/), [PerfView](https://github.com/Microsoft/perfview), and [Visual Studio Profiler](https://learn.microsoft.com/visualstudio/profiling/profiling-feature-tour). This new profiler is available from the 0.12.1 version.

![](https://wojciechnagorski.com/images/EventPipeProfiler/SpeedScopeAdvance.png)

# Configuration

`EventPipeProfiler` can be enabled in three ways:

1. Using parameter `-p EP` or `--profiler EP` from the console line.
2. Marking the benchmarked class with `[EventPipeProfiler(...)]` attribute. You can find an example below.
3. Using a custom configuration. You can find an example below.

[!include[IntroEventPipeProfiler](../samples/IntroEventPipeProfiler.md)]
[!include[IntroEventPipeProfilerAdvanced](../samples/IntroEventPipeProfilerAdvanced.md)]