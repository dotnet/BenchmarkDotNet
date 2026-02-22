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

1. Run all benchmarks from System.Memory namespace: `-f 'System.Memory*'`
2. Run all benchmarks: `-f '*'`
3. Run all benchmarks from ClassA and ClassB `-f '*ClassA*' '*ClassB*'`

**Note**: If you would like to **join** all the results into a **single summary**, you need to put `--join`. For example: `-f '*ClassA*' '*ClassB*' --join`

## List of benchmarks

The `--list` allows you to print all of the available benchmark names. Available options are:

* `flat` - prints list of the available benchmarks: `--list flat`

```ini
BenchmarkDotNet.Samples.Algo_Md5VsSha256.Md5
BenchmarkDotNet.Samples.Algo_Md5VsSha256.Sha256
BenchmarkDotNet.Samples.IntroArguments.Benchmark
BenchmarkDotNet.Samples.IntroArgumentsSource.SingleArgument
BenchmarkDotNet.Samples.IntroArgumentsSource.ManyArguments
BenchmarkDotNet.Samples.IntroArrayParam.ArrayIndexOf
BenchmarkDotNet.Samples.IntroArrayParam.ManualIndexOf
BenchmarkDotNet.Samples.IntroBasic.Sleep
[...]
```

* `tree` - prints tree of the available benchmarks: `--list tree`

```ini
BenchmarkDotNet
 └─Samples
    ├─Algo_Md5VsSha256
    │  ├─Md5
    │  └─Sha256
    ├─IntroArguments
    │  └─Benchmark
    ├─IntroArgumentsSource
    │  ├─SingleArgument
    │  └─ManyArguments
    ├─IntroArrayParam
    │  ├─ArrayIndexOf
    │  └─ManualIndexOf
    ├─IntroBasic
    │  ├─Sleep
[...]
```

The `--list` option works with the `--filter` option. Examples:

* `--list flat --filter *IntroSetupCleanup*` prints:

```ini
BenchmarkDotNet.Samples.IntroSetupCleanupGlobal.Logic
BenchmarkDotNet.Samples.IntroSetupCleanupIteration.Benchmark
BenchmarkDotNet.Samples.IntroSetupCleanupTarget.BenchmarkA
BenchmarkDotNet.Samples.IntroSetupCleanupTarget.BenchmarkB
BenchmarkDotNet.Samples.IntroSetupCleanupTarget.BenchmarkC
BenchmarkDotNet.Samples.IntroSetupCleanupTarget.BenchmarkD
```

* `--list tree --filter *IntroSetupCleanup*` prints:

```ini
BenchmarkDotNet
 └─Samples
    ├─IntroSetupCleanupGlobal
    │  └─Logic
    ├─IntroSetupCleanupIteration
    │  └─Benchmark
    └─IntroSetupCleanupTarget
       ├─BenchmarkA
       ├─BenchmarkB
       ├─BenchmarkC
       └─BenchmarkD
```

## Categories

You can also filter the benchmarks by categories:

* `--anyCategories` - runs all benchmarks that belong to **any** of the provided categories
* `--allCategories`- runs all benchmarks that belong to **all** provided categories

## Diagnosers

* `-m`, `--memory` - enables MemoryDiagnoser and prints memory statistics
* `-t`, `--threading` - enables `ThreadingDiagnoser` and prints threading statistics
* `-d`, `--disasm`- enables DisassemblyDiagnoser and exports diassembly of benchmarked code. When you enable this option, you can use:
  * `--disasmDepth` - Sets the recursive depth for the disassembler.
  * `--disasmDiff` - Generates diff reports for the disassembler.

## Runtimes

The `--runtimes` or just `-r` allows you to run the benchmarks for selected Runtimes. Available options are:

* Clr - BDN will either use Roslyn (if you run it as .NET app) or latest installed .NET SDK to build the benchmarks (if you run it as .NET Core app).
* Core - if you run it as .NET Core app, BDN will use the same target framework moniker, if you run it as .NET app it's going to use net8.0.
* Mono - it's going to use the Mono from `$Path`, you can override  it with `--monoPath`.
* net46, net461, net462, net47, net471, net472, net48, net481 - to build and run benchmarks against specific .NET Framework version.
* netcoreapp3.1, net5.0, net6.0, net7.0, net8.0 - to build and run benchmarks against specific .NET (Core) version.
* nativeaot5.0, nativeaot6.0, nativeaot7.0, nativeaot8.0 - to build and run benchmarks using NativeAOT. Can be customized with additional options: `--ilcPackages`, `--ilCompilerVersion`.
* mono6.0, mono7.0, mono8.0 - to build and run benchmarks with .Net 6+ using MonoVM.

Example: run the benchmarks for .NET 4.7.2 and .NET 8.0:

```log
dotnet run -c Release -- --runtimes net472 net8.0
```

Example: run the benchmarks for .NET Core 3.1 and latest .NET SDK installed on your PC:

```log
dotnet run -c Release -f netcoreapp3.1 -- --runtimes clr core
```

But same command executed with `-f net6.0` is going to run the benchmarks for .NET 6.0:

```log
dotnet run -c Release -f net6.0 -- --runtimes clr core
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

## Response files support

Benchmark.NET supports parsing parameters via response files. for example you can create file `run.rsp` with following content
```
--warmupCount 1
--minIterationCount 9
--maxIterationCount 12
```

and run it using `dotnet run -c Release -- @run.rsp`. It would be equivalent to running following command line

```log
dotnet run -c Release -- --warmupCount 1 --minIterationCount 9 --maxIterationCount 12
```

## Statistical Test

To perform a Mann–Whitney U Test and display the results in a dedicated column you need to provide the Threshold:

* `--statisticalTest`- Threshold for Mann–Whitney U Test. Examples: 5%, 10ms, 100ns, 1s

Example: run Mann–Whitney U test with relative ratio of 5% for all benchmarks for .NET 6.0 (base) vs .NET 8.0 (diff). .NET 6.0 will be baseline because it was first.

```log
dotnet run -c Release -- --filter * --runtimes net6.0 net8.0 --statisticalTest 5%
```

## Example Usages

* Use Job.ShortRun for running the benchmarks:  
    `-d -j short`
* Run benchmarks in process:  
    `-d -i`
* Run benchmarks for .NET 4.7.2, .NET 8.0 and Mono. .NET 4.7.2 will be baseline because it was first.:
    `--disasm --runtimes net472 net8.0 Mono`
* Run benchmarks for .NET Core 3.1, .NET 6.0 and .NET 8.0. .NET Core 3.1 will be baseline because it was first.:
    `--disasm --runtimes netcoreapp3.1 net6.0 net8.0`
* Use MemoryDiagnoser to get GC stats:
    `-d -m`
* Use DisassemblyDiagnoser to get disassembly:
    `-d`
* Use HardwareCountersDiagnoser to get hardware counter info:
    `--counters CacheMisses+InstructionRetired --disasm`
* Run all benchmarks exactly once:
    `-d -f * -j Dry`
* Run all benchmarks from System.Memory namespace:
    `-d -f System.Memory*`
* Run all benchmarks from ClassA and ClassB using type names:
    `-d -f ClassA ClassB`
* Run all benchmarks from ClassA and ClassB using patterns:
    `-d -f *.ClassA.* *.ClassB.*`
* Run all benchmarks called `BenchmarkName` and show the results in single summary:
    `--disasm --filter *.BenchmarkName --join`
* Run selected benchmarks once per iteration:
    `--disasm --runOncePerIteration`
* Run selected benchmarks 100 times per iteration. Perform single warmup iteration and 5 actual workload iterations:
    `--disasm --invocationCount 100 --iterationCount 5 --warmupCount 1`
* Run selected benchmarks 250ms per iteration. Perform from 9 to 15 iterations:
    `--disasm --iterationTime 250 --maxIterationCount 15 --minIterationCount 9`
* Run MannWhitney test with relative ratio of 5% for all benchmarks for .NET 6.0 (base) vs .NET 8.0 (diff). .NET Core 6.0 will be baseline because it was provided as first.:
    `--disasm --filter * --runtimes net6.0 net8.0 --statisticalTest 5%`
* Run benchmarks using environment variables 'ENV_VAR_KEY_1' with value 'value_1' and 'ENV_VAR_KEY_2' with value 'value_2':
    `--disasm --envVars ENV_VAR_KEY_1:value_1 ENV_VAR_KEY_2:value_2`
* Hide Mean and Ratio columns (use double quotes for multi-word columns: "Alloc Ratio"):
    `-d -h Mean Ratio`

## More

* `-j`, `--job`               (Default: Default) Dry/Short/Medium/Long or Default
* `-r`, `--runtimes`          Full target framework moniker for .NET Core and .NET. For Mono just 'Mono'. For NativeAOT please append target runtime version (example: 'nativeaot7.0'). First one will be marked as baseline!
* `-e`, `--exporters`         GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML
* `-m`, `--memory`            (Default: false) Prints memory statistics
* `-t`, `--threading`         (Default: false) Prints threading statistics
* `--exceptions`              (Default: false) Prints exception statistics
* `-d, --disasm`              (Default: false) Gets disassembly of benchmarked code
* `-p, --profiler`            Profiles benchmarked code using selected profiler. Available options: EP/ETW/CV/NativeMemory
* `-f, --filter`              Glob patterns
* `-h, --hide`                Hides columns by name
* `-i, --inProcess`           (Default: false) Run benchmarks in Process
* `-a, --artifacts`           Valid path to accessible directory
* `--outliers`                (Default: RemoveUpper) `DontRemove`/`RemoveUpper`/`RemoveLower`/`RemoveAll`
* `--affinity`                Affinity mask to set for the benchmark process
* `--allStats`                (Default: false) Displays all statistics (min, max & more)
* `--allCategories`           Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed
* `--anyCategories`           Any Categories to run
* `--attribute`               Run all methods with given attribute (applied to class or method)
* `--join`                    (Default: false) Prints single table with results for all benchmarks
* `--keepFiles`               (Default: false) Determines if all auto-generated files should be kept or removed after running the benchmarks.
* `--noOverwrite`             (Default: false) Determines if the exported result files should not be overwritten (be default they are overwritten).
* `--counters`                Hardware Counters
* `--cli`                     Path to dotnet cli (optional).
* `--packages`                The directory to restore packages to (optional).
* `--coreRun`                 Path(s) to CoreRun (optional).
* `--monoPath`                Optional path to Mono which should be used for running benchmarks.
* `--clrVersion`              Optional version of private CLR build used as the value of `COMPLUS_Version` env var.
* `--ilCompilerVersion`       Optional version of Microsoft.DotNet.ILCompiler which should be used to run with NativeAOT. Example: "7.0.0-preview.3.22123.2"
* `--ilcPackages`             Optional path to shipping packages produced by local dotnet/runtime build. Example: 'D:\projects\runtime\artifacts\packages\Release\Shipping\'
* `--launchCount`             How many times we should launch process with target benchmark. The default is 1.
* `--warmupCount`             How many warmup iterations should be performed. If you set it, the minWarmupCount and maxWarmupCount are ignored. By default calculated by the heuristic.
* `--minWarmupCount`          Minimum count of warmup iterations that should be performed. The default is 6.
* `--maxWarmupCount`          Maximum count of warmup iterations that should be performed. The default is 50.
* `--iterationTime`           Desired time of execution of an iteration in milliseconds. Used by Pilot stage to estimate the number of invocations per iteration. 500ms by default
* `--iterationCount`          How many target iterations should be performed. By default calculated by the heuristic.
* `--minIterationCount`       Minimum number of iterations to run. The default is 15.
* `--maxIterationCount`       Maximum number of iterations to run. The default is 100.
* `--invocationCount`         Invocation count in a single iteration. By default calculated by the heuristic.
* `--unrollFactor`            How many times the benchmark method will be invoked per one iteration of a generated loop. 16 by default
* `--strategy`                The RunStrategy that should be used. Throughput/ColdStart/Monitoring.
* `--platform`                The Platform that should be used. If not specified, the host process platform is used (default). AnyCpu/X86/X64/Arm/Arm64/LoongArch64.
* `--runOncePerIteration`     (Default: false) Run the benchmark exactly once per iteration.
* `--info`                    (Default: false) Print environment information.
* `--apples`                  (Default: false) Runs apples-to-apples comparison for specified Jobs.
* `--list`                    (Default: Disabled) Prints all of the available benchmark names. Flat/Tree
* `--disasmDepth`             (Default: 1) Sets the recursive depth for the disassembler.
* `--disasmFilter`            Glob patterns applied to full method signatures by the the disassembler.
* `--disasmDiff`              (Default: false) Generates diff reports for the disassembler.
* `--logBuildOutput`          Log Build output.
* `--generateBinLog`          Generate msbuild `binlog` for builds
* `--buildTimeout`            Build timeout in seconds.
* `--wakeLock`                Prevents the system from entering sleep or turning off the display. None/System/Display.
* `--stopOnFirstError`        (Default: false) Stop on first error.
* `--statisticalTest`         Threshold for Mann–Whitney U Test. Examples: 5%, 10ms, 100ns, 1s
* `--disableLogFile`          Disables the `logfile`.
* `--maxWidth`                Max parameter column width, the default is 20.
* `--envVars`                 Colon separated environment variables (key:value)
* `--memoryRandomization`     Specifies whether Engine should allocate some random-sized memory between iterations. It makes [GlobalCleanup] and [GlobalSetup] methods to be executed after every iteration.
* `--wasmEngine`              Full path to a java script engine used to run the benchmarks, used by Wasm toolchain.
* `--wasmArgs`                (Default: --expose_wasm) Arguments for the javascript engine used by Wasm toolchain.
* `--customRuntimePack`       Path to a custom runtime pack. Only used for wasm/MonoAotLLVM currently.
* `--AOTCompilerPath`         Path to Mono AOT compiler, used for MonoAotLLVM.
* `--AOTCompilerMode`         (Default: mini) Mono AOT compiler mode, either 'mini' or 'llvm'
* `--wasmDataDir`             Wasm data directory
* `--wasmCoreCLR`             (Default: false) Use CoreCLR runtime pack (Microsoft.NETCore.App.Runtime.browser-wasm) instead of the Mono runtime pack for WASM benchmarks.
* `--noForcedGCs`             Specifying would not forcefully induce any GCs.
* `--noOverheadEvaluation`    Specifying would not run the evaluation overhead iterations.
* `--resume`                  (Default: false) Continue the execution if the last run was stopped.
* `--help`                    Display this help screen.
* `--version`                 Display version information.
