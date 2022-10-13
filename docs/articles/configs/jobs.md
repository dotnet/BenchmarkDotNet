---
uid: docs.jobs
name: Jobs
---

# Jobs

Basically, a *job* describes how to run your benchmark. Practically, it's a set of characteristics which can be specified. You can specify one or several jobs for your benchmarks.

## Characteristics

There are several categories of characteristics which you can specify. Let's consider each category in detail.

### Id

It's a single string characteristic. It allows to name your job. This name will be used in logs and a part of a folder name with generated files for this job. `Id` doesn't affect benchmark results, but it can be useful for diagnostics. If you don't specify `Id`, random value will be chosen based on other characteristics

### Environment

`Environment` specifies an environment of the job. You can specify the following characteristics:

* `Platform`: `x86` or `x64`
* `Runtime`:
  * `Clr`: Full .NET Framework (available only on Windows)
  * `Core`: CoreCLR (x-plat)
  * `Mono`: Mono (x-plat)
* `Jit`:
  * `LegacyJit` (available only for `Runtime.Clr`)
  * `RyuJit` (available only for `Runtime.Clr` and `Runtime.Core`)
  * `Llvm` (available only for `Runtime.Mono`)
* `Affinity`: [Affinity](https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx) of a benchmark process
* `GcMode`: settings of Garbage Collector
  * `Server`: `true` (Server mode) or `false` (Workstation mode)
  * `Concurrent`:  `true` (Concurrent mode) or `false` (NonConcurrent mode)
  * `CpuGroups`:  Specifies whether garbage collection supports multiple CPU groups
  * `Force`: Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
  * `AllowVeryLargeObjects`:  On 64-bit platforms, enables arrays that are greater than 2 gigabytes (GB) in total size
* `LargeAddressAware`: Specifies that benchmark can handle addresses larger than 2 gigabytes. See also: @BenchmarkDotNet.Samples.IntroLargeAddressAware and [`LARGEADDRESSAWARE`](https://learn.microsoft.com/cpp/build/reference/largeaddressaware-handle-large-addresses)
  * `false`: Benchmark uses the defaults (64-bit: enabled; 32-bit: disabled).
  * `true`: Explicitly specify that benchmark can handle addresses larger than 2 gigabytes.
* `EnvironmentVariables`: customized environment variables for target benchmark. See also: @BenchmarkDotNet.Samples.IntroEnvVars

BenchmarkDotNet will use host process environment characteristics for non specified values.

### Run

In this category, you can specify how to benchmark each method.

* `RunStrategy`:
  * `Throughput`: default strategy which allows to get good precision level
  * `ColdStart`: should be used only for measuring cold start of the application or testing purpose
  * `Monitoring`: A mode without overhead evaluating, with several target iterations
* `LaunchCount`: how many times we should launch process with target benchmark
* `WarmupCount`: how many warmup iterations should be performed
* `IterationCount`: how many target iterations should be performed (if specified, `BenchmarkDotNet.Jobs.RunMode.MinIterationCount` and `BenchmarkDotNet.Jobs.RunMode.MaxIterationCount` will be ignored)
* `IterationTime`: desired time of a single iteration
* `UnrollFactor`: how many times the benchmark method will be invoked per one iteration of a generated loop
* `InvocationCount`: count of invocation in a single iteration (if specified, `IterationTime` will be ignored), must be a multiple of `UnrollFactor`
* `MinIterationCount`: Minimum count of target iterations that should be performed, the default value is 15
* `MaxIterationCount`: Maximum count of target iterations that should be performed, the default value is 100
* `MinWarmupIterationCount`: Minimum count of warmup iterations that should be performed, the default value is 6
* `MaxWarmupIterationCount`: Maximum count of warmup iterations that should be performed, the default value is 50

Usually, you shouldn't specify such characteristics like `LaunchCount`, `WarmupCount`, `TargetCount`, or `IterationTime` because BenchmarkDotNet has a smart algorithm to choose these values automatically based on received measurements. You can specify it for testing purposes or when you are damn sure that you know the right characteristics for your benchmark (when you set `TargetCount` = `20` you should understand why `20` is a good value for your case).

### Accuracy

If you want to change the accuracy level, you should use the following characteristics instead of manually adjusting values of `WarmupCount`, `TargetCount`, and so on.

* `MaxRelativeError`, `MaxAbsoluteError`: Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error). *In these two characteristics*, the error means half of 99.9% confidence interval. `MaxAbsoluteError` is an absolute `TimeInterval`; doesn't have a default value. `MaxRelativeError` defines max acceptable (`(<half of CI 99.9%>) / Mean`).
* `MinIterationTime`: Minimum time of a single iteration. Unlike `Run.IterationTime`, this characteristic specifies only the lower limit. In case of need, BenchmarkDotNet can increase this value.
* `MinInvokeCount`:  Minimum about of target method invocation. Default value if `4` but you can decrease this value for cases when single invocations takes a lot of time.
* `EvaluateOverhead`: if your benchmark method takes nanoseconds, BenchmarkDotNet overhead can significantly affect measurements. If this characteristic is enabled, the overhead will be evaluated and subtracted from the result measurements. Default value is `true`.
* `WithOutlierMode`: sometimes you could have outliers in your measurements. Usually these are unexpected outliers which arose because of other processes activities. By default (`OutlierMode.RemoveUpper`), all upper outliers (which is larger than Q3) will be removed from the result measurements. However, some of benchmarks have *expected* outliers. In these situation, you expect that some of invocation can produce outliers measurements (e.g. in case of network activities, cache operations, and so on). If you want to see result statistics with these outliers, you should use `OutlierMode.DontRemove`. If you can also choose `OutlierMode.RemoveLower` (outliers which are smaller than Q1 will be removed) or `OutlierMode.RemoveAll` (all outliers will be removed). See also: @BenchmarkDotNet.Mathematics.OutlierMode
* `AnalyzeLaunchVariance`: this characteristic makes sense only if `Run.LaunchCount` is default. If this mode is enabled and, BenchmarkDotNet will try to perform several launches and detect if there is a variance between launches. If this mode is disable, only one launch will be performed.

### Infrastructure

Usually, you shouldn't specify any characteristics from this section, it can be used for advanced cases only.

* `Toolchain`: a toolchain which generates source code for target benchmark methods, builds it, and executes it. BenchmarkDotNet has own toolchains for .NET, .NET Core, Mono and CoreRT projects. If you want, you can define own toolchain.
* `Clock`: a clock which will be used for measurements. BenchmarkDotNet automatically choose the best available clock source, but you can specify own clock source.
* `EngineFactory`: a provider for measurement engine which performs all the measurement magic. If you don't trust BenchmarkDotNet, you can define own engine and implement all the measurement stages manually.

## Usage

There are several ways to specify a job.

### Object style

You can create own jobs directly from the source code via a custom config:

```cs
[Config(typeof(Config))]
public class MyBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(
                new Job("MySuperJob", RunMode.Dry, EnvMode.RyuJitX64)
                {
                    Env = { Runtime = Runtime.Core },
                    Run = { LaunchCount = 5, IterationTime = TimeInterval.Millisecond * 200 },
                    Accuracy = { MaxStdErrRelative = 0.01 }
                });

            // The same, using the .With() factory methods:
            Add(
                Job.Dry
                .WithPlatform(Platform.X64)
                .WithJit(Jit.RyuJit)
                .WithRuntime(Runtime.Core)
                .WithLaunchCount(5)
                .WithIterationTime(TimeInterval.Millisecond * 200)
                .WithMaxRelativeError(0.01)
                .WithId("MySuperJob"));
        }
    }
    // Benchmarks
}
```

Basically, it's a good idea to start with predefined values (e.g. `EnvMode.RyuJitX64` and `RunMode.Dry` passed as constructor args) and modify rest of the properties using property setters or with help of object initializer syntax.

Note that the job cannot be modified after it's added into config. Trying to set a value on property of the frozen job will throw an `InvalidOperationException`. Use the `Job.Frozen` property to determine if the code properties can be altered.

If you do want to create a new job based on frozen one (all predefined job values are frozen) you can use the `.With()` extension method

```cs
            var newJob = Job.Dry.With(Platform.X64);
```

or pass the frozen value as a constructor argument

```c#
            var newJob = new Job(Job.Dry) { Env = { Platform = Platform.X64 } };
```

or use the `.Apply()` method on unfrozen job

```c#
            var newJob = new Job() { Env = { Platform = Platform.X64 } }.Apply(Job.Dry);
```

in any case the Id property will not be transfered and you must pass it explicitly (using the .ctor id argument or the `.WithId()` extension method).

### Attribute style

You can also add new jobs via attributes. Examples:

```cs
[DryJob]
[ClrJob, CoreJob, MonoJob]
[LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 5, targetCount: 5, id: "FastAndDirtyJob")]
public class MyBenchmarkClass
```

Note that each of the attributes identifies a separate job, the sample above will result in 8 different jobs, not a single merged job.

### Attribute style for merging jobs

Sometimes you want to apply some changes to other jobs, without adding a new job to a config (which results in one extra benchmark run).

To do that you can use following predefined job mutator attributes:

* `[EvaluateOverhead]`
* `[GcConcurrent]`
* `[GcForce]`
* `[GcServer]`
* `[InnerIterationCount]`
* `[InvocationCount]`
* `[IterationCount]`
* `[IterationTime]`
* `[MaxAbsoluteError]`
* `[MaxIterationCount]`
* `[MaxRelativeError]`
* `[MinInvokeCount]`
* `[MinIterationCount]`
* `[MinIterationTime]`
* `[Outliers]`
* `[ProcessCount]`
* `[RunOncePerIteration]`
* `[WarmupCount]`
* `[MinWarmupCount]`
* `[MaxWarmupCount]`

So following example:

```cs
[ClrJob, CoreJob]
[GcServer(true)]
public class MyBenchmarkClass
```

Is going to be merged to a config with two jobs:

* CoreJob with `GcServer=true`
* ClrJob with `GcServer=true`

#### Custom attributes

You can also create your own custom attributes with your favourite set of jobs. Example:

```cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
public class MySuperJobAttribute : Attribute, IConfigSource
{
    protected MySuperJobAttribute()
    {
        var job = new Job("MySuperJob", RunMode.Core);
        job.Env.Platform = Platform.X64;
        Config = ManualConfig.CreateEmpty().With(job);
    }

    public IConfig Config { get; }
}

[MySuperJob]
public class MyBenchmarks
```

---

[!include[IntroGcMode](../samples/IntroGcMode.md)]
