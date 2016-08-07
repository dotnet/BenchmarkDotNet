# Jobs

A *job* is an environment for your benchmarks. You can set one or several jobs for your set of benchmarks.

## Characteristics

* **Toolchain.** A toolchain for generating/building/executing your benchmark. Values: `Classic` (Roslyn based) *[default]* and `Core` (dotnet cli based) .
* **Mode.** Values: `Throughput` *[default]*, `SingleRun`.
* **Platform.** Values: `Host` *[default]*, `AnyCpu`, `X86`, `X64`.
* **Jit.** Values: `Host` *[default]*, `LegacyJit`, `RyuJit`.
* **Framework.** Values: `Host` *[default]*, `V40`, `V45`, `V451`, `V452`, `V46`.
* **Runtime.** Values: `Host` *[default]*, `Clr`, `Mono`, `Core`.
* **LaunchCount.** Count of separated process launches. Values: `Auto` *[default]* or specific number.
* **WarmupCount.** Count of warmup iterations. Values: `Auto` *[default]* or specific number.
* **TargetCount.** Count of target iterations (that will be used for summary). Values: `Auto` *[default]* or specific number.
* **IterationTime.** Desired time of execution of an iteration (in ms). Values: `Auto` *[default]* or specific number.
* **Affinity.** [ProcessorAffinity](https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx) of process. Values: `Auto` *[default]* or specific mask.

The `Host` value means that value will be inherited from host process settings. The `Auto` values means the BenchmarkDotNet automatically choose the best value.

## Predefined jobs

```cs
class Job
{
    IJob Default = new Job();
    IJob LegacyX86 = new Job { Platform = Platform.X86, Jit = Jit.LegacyJit };
    IJob LegacyX64 = new Job { Platform = Platform.X64, Jit = Jit.LegacyJit };
    IJob RyuJitX64 = new Job { Platform = Platform.X64, Jit = Jit.RyuJit };
    IJob Dry = new Job { Mode = Mode.SingleRun, ProcessCount = 1, WarmupCount = 1, TargetCount = 1 };
    IJob[] AllJits = { LegacyX86, LegacyX64, RyuJitX64 };
    IJob Clr = new Job { Runtime = Runtime.Clr };
    IJob Mono = new Job { Runtime = Runtime.Mono };
    IJob Core = new Job { Runtime = Runtime.Core };
    IJob LongRun = new Job { LaunchCount = 3, WarmupCount = 30, TargetCount = 1000 };
}
```

##Examples

```cs
// *** Command style ***
[Config("jobs=AllJits")]
[Config("jobs=Dry")]
[Config("jobs=LegacyX64,RyuJitX64")]
```

```cs
// *** Object style ***
class Config : ManualConfig
{
    public Config()
    {
    	Add(Job.AllJits);
    	Add(Job.LegacyX64, Job.RyuJitX64);
        Add(Job.Default.With(Mode.SingleRun).WithProcessCount(1).WithWarmupCount(1).WithTargetCount(1));
        Add(Job.Default.With(Framework.V40).With(Runtime.Mono).With(Platform.X64));
    }
}
```
