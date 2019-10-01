---
uid: docs.configoptions
name: Configoptions
---

# Config Options

The config options let you customize some behavior of BenchmarkDotNet - mainly regarding the output.
Available config options are:

* `ConfigOptions.Default` - No configuration option is set - this is the default.
* `ConfigOptions.KeepBenchmarkFiles` - All auto-generated files should be kept after running the benchmarks (be default they are removed).
* `ConfigOptions.JoinSummary` - All benchmarks results should be joined into a single summary (by default we have a summary per type).
* `ConfigOptions.StopOnFirstError` - Benchmarking should be stopped after the first error (by default it's not).
* `ConfigOptions.DisableOptimizationsValidator` - Mandatory optimizations validator should be entirely turned off.
* `ConfigOptions.DontOverwriteResults` - The exported result files should not be overwritten (be default they are overwritten).
* `ConfigOptions.DisableLogFile` - Disables the log file written on disk.

All of these options could be combined and are available as CLI (Comand Line Interface) option (except `DisableOptimizationsValidator`), see [Console Arguments](xref:docs.console-args) for further information how to use the CLI.

Any of these options could be used either in `object style config` or `fluent style config`:

### Object style config

```cs
public class Config : ManualConfig
{
    public Config()
    {
        Options.Set(true, ConfigOptions.JoinSummary);
        Options.Set(true, ConfigOptions.DisableLogFile);

        // or
        Options.Set(true, ConfigOptions.JoinSummary | ConfigOptions.DisableLogFile);

        // or using the With() factory method:
        this.With(ConfigOptions.JoinSummary)
            .With(ConfigOptions.DisableLogFile);

    }
}
```

### Fluent style config

```cs
    public static void Run()
    {
        BenchmarkRunner
            .Run<Benchmarks>(
                ManualConfig
                    .Create(DefaultConfig.Instance)
                    .With(ConfigOptions.JoinSummary)
                    .With(ConfigOptions.DisableLogFile)
                    // or
                    .With(ConfigOptions.JoinSummary | ConfigOptions.DisableLogFile));
    }
```