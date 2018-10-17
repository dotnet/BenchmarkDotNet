---
uid: docs.console-args
name: Console Arguments
---

# How to use console arguments

`BenchmarkSwitcher` supports various console arguments, to make it work you need to pass the `args` to switcher:

```cs
class Program
{
    static void Main(string[] args) 
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
```

**Note:** the docs that you are currently reading might get outdated, to get the most up-to-date info about supported console arguments run the benchmarks with `--help`.

## Filter

The `--filter` or just `-f` allows you to filter the benchmarks by their full name (`namespace.typeName.methodName`) using glob patterns.

Examples:

1. Run all benchmarks from System.Memory namespace: `-f System.Memory*`
2. Run all benchmarks: `-f *`
3. Run all benchmarks from ClassA and ClassB `-f *ClassA* *ClassB*`

**Note**: If you would like to **join** all the results into a **single summary**, you need to us `--join`.

## Categories

You can also filter the benchmarks by categories:

* `--anyCategories` - runs all benchmarks that belong to **any** of the provided categories
* `--allCategories`- runs all benchmarks that belong to **all** provided categories

## Diagnosers

* `-m`, `--memory` - enables MemoryDiagnoser and prints memory statistics
* `-d`, `--disassm`- enables DisassemblyDiagnoser and exports diassembly of benchmarked code

## Runtimes

The `--runtimes` or just `-r` allows you to run the benchmarks for selected Runtimes. Available options are: 

* Clr - BDN will either use Roslyn (if you run it as .NET app) or latest installed .NET SDK to build the benchmarks (if you run it as .NET Core app)
* Core - if you run it as .NET Core app, BDN will use the same target framework moniker, if you run it as .NET app it's going to use netcoreapp2.0
* Mono - it's going to use the Mono from `$Path`, you can override  it with `--monoPath`
* CoreRT - it's going to use latest CoreRT. Can be customized with additional options: `--ilcPath`, `--coreRtVersion` 
* net46, net461, net462, net47, net471, net472 - to build and run benchmarks against specific .NET framework version 
* netcoreapp2.0, netcoreapp2.1, netcoreapp2.2, netcoreapp3.0 - to build and run benchmarks against specific .NET Core version

Example: run the benchmarks for .NET 4.7.2 and .NET Core 2.1:

```log
dotnet run -c Release -- --runtimes net472 netcoreapp2.1
```

Example: run the benchmarks for .NET Core 3.0 and latest .NET SDK installed on your PC:

```log
dotnet run -c Release -f netcoreapp3.0 -- --runtimes clr core
```

But same command executed with `-f netcoreapp2.0` is going to run the benchmarks for .NET Core 2.0:

```log
dotnet run -c Release -f netcoreapp2.0 -- --runtimes clr core
```

## Number of invocations and iterations

* `--launchCount` - how many times we should launch process with target benchmark. The default is 1.
* `--warmupCount` - how many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic.
* `--minWarmupCount` - minimum count of warmup iterations that should be performed. The default is 6.
* `--maxWarmupCount` - maximum count of warmup iterations that should be performed. The default is 50.
* `--iterationTime` - desired time of execution of an iteration. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default.
* `--iterationCount` - how many target iterations should be performed. By default calculated by the heuristic.
* `--minIterationCount` - minimum number of iterations to run. The default is 15.
* `--maxIterationCount` - maximum number of iterations to run. The default is 100.
* `--invocationCount` - invocation count in a single iteration. By default calculated by the heuristic.
* `--unrollFactor` - how many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default
* `--runOncePerIteration` - run the benchmark exactly once per iteration. False by default.

Example: run single warmup iteration, from 9 to 12 actual workload iterations.

```log
dotnet run -c Release -- --warmupCount 1 --minIterationCount 9 --maxIterationCount 12
```

## Specifying custom default settings for console argument parser

If you want to have a possibility to specify custom default Job settings programmatically and optionally overwrite it with console line arguments, then you should create a global config with single job marked as `.AsDefault` and pass it to `BenchmarkSwitcher` together with the console line arguments.

Example: run single warmup iteration by default.

```cs
static void Main(string[] args)
    => BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(args, GetGlobalConfig());

static IConfig GetGlobalConfig()
    => DefaultConfig.Instance
        .With(Job.Default
            .WithWarmupCount(1)
            .AsDefault()); // the KEY to get it working
```

Now, the default settings are: `WarmupCount=1` but you might still overwrite it from console args like in the example below: 

```log
dotnet run -c Release -- --warmupCount 2
```

## More

* `-j`, `--job` (Default: Default) Dry/Short/Medium/Long or Default
* `-e`, `--exporters` GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML
* `-i`, `--inProcess` (Default: false) Run benchmarks in Process
* `-a`, `--artifacts` Valid path to accessible directory
* `--outliers` (Default: OnlyUpper) None/OnlyUpper/OnlyLower/All
* `--affinity` Affinity mask to set for the benchmark process
* `--allStats` (Default: false) Displays all statistics (min, max & more)
* `--attribute` Run all methods with given attribute (applied to class or method)
* `--monoPath` custom Path for Mono
* `--cliPath` custom Path for dotnet cli
* `--coreRt` path to ILCompiler for CoreRT
* `--info` prints environment configuration including BenchmarkDotNet, OS, CPU and .NET version
