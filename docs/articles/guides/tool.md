---
uid: docs.tool
name: Command-line tool
---

# Command-line tool

BenchmarkDotNet is also available as a [.NET Core global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)
  and provides a convenient way to execute your benchmarks from the command line interface.

## Installation

Download and install the [.NET Core 2.1 SDK](https://www.microsoft.com/net/download) or newer.
Once installed, run the following command to install the [BenchmarkDotNet.Tool](https://www.nuget.org/packages/BenchmarkDotNet.Tool/) NuGet package:

```log
dotnet tool install -g BenchmarkDotNet.Tool
```

If you already have a previous version of installed, you can upgrade to the latest version using the following command:

```log
dotnet tool update -g BenchmarkDotNet.Tool
```

If you want to remove the tool:

```log
dotnet tool uninstall -g BenchmarkDotNet.Tool
```

If you want to get the list of install global tools:

```log
dotnet tool list -g
```

## Usage

The basic usage syntax is:

```log
dotnet benchmark [arguments] [options]
```

For example, the following example scans the `MyAssemblyWithBenchmarks.dll` for benchmarks and lets you select which benchmarks to execute:

```log
dotnet benchmark MyAssemblyWithBenchmarks.dll
```

To execute all benchmarks use `--filter *`:

```log
dotnet benchmark MyAssemblyWithBenchmarks.dll --filter *
```

For further arguments for the `BenchmarkSwitcher` see also [Console Arguments](console-args.md).

## Help

```md
A dotnet tool to execute benchmarks built with BenchmarkDotNet.

Usage: benchmark [arguments] [options]

Arguments:
  AssemblyFile  The assembly with the benchmarks (required).

Options:
  --version     Show version information
  -?|-h|--help  Show help information

The first argument in [arguments] is the benchmark assembly and every following argument is passed to the BenchmarkSwitcher.
BenchmarkSwitcher arguments:

BenchmarkDotNet.Tool 0.12.0.0
.NET Foundation and contributors
USAGE:
Use Job.ShortRun for running the benchmarks:
   -j short
Run benchmarks in process:
   -i
Run benchmarks for .NET 4.7.2, .NET Core 2.1 and Mono. .NET 4.7.2 will be 
baseline because it was first.:
   --runtimes net472 netcoreapp2.1 Mono
Run benchmarks for .NET Core 2.0, .NET Core 2.1 and .NET Core 2.2. .NET Core 
2.0 will be baseline because it was first.:
   --runtimes netcoreapp2.0 netcoreapp2.1 netcoreapp2.2
Use MemoryDiagnoser to get GC stats:
   -m
Use DisassemblyDiagnoser to get disassembly:
   -d
Use HardwareCountersDiagnoser to get hardware counter info:
   --counters CacheMisses+InstructionRetired
Run all benchmarks exactly once:
   -f '*' -j Dry
Run all benchmarks from System.Memory namespace:
   -f 'System.Memory*'
Run all benchmarks from ClassA and ClassB using type names:
   -f ClassA ClassB
Run all benchmarks from ClassA and ClassB using patterns:
   -f '*.ClassA.*' '*.ClassB.*'
Run all benchmarks called `BenchmarkName` and show the results in single 
summary:
   --filter '*.BenchmarkName' --join
Run selected benchmarks once per iteration:
   --runOncePerIteration
Run selected benchmarks 100 times per iteration. Perform single warmup 
iteration and 5 actual workload iterations:
   --invocationCount 100 --iterationCount 5 --warmupCount 1
Run selected benchmarks 250ms per iteration. Perform from 9 to 15 iterations:
   --iterationTime 250 --maxIterationCount 15 --minIterationCount 9
Run MannWhitney test with relative ratio of 5% for all benchmarks for .NET Core
 2.0 (base) vs .NET Core 2.1 (diff). .NET Core 2.0 will be baseline because it 
was provided as first.:
   --filter * --runtimes netcoreapp2.0 netcoreapp2.1 --statisticalTest 5%

  -j, --job                (Default: Default) Dry/Short/Medium/Long or Default

  -r, --runtimes           Full target framework moniker for .NET Core and 
                           .NET. For Mono just 'Mono', for CoreRT just 
                           'CoreRT'. First one will be marked as baseline!

  -e, --exporters          GitHub/StackOverflow/RPlot/CSV/JSON/HTML/XML

  -m, --memory             (Default: false) Prints memory statistics

  -t, --threading          (Default: false) Prints threading statistics

  -d, --disasm             (Default: false) Gets disassembly of benchmarked 
                           code

  -p, --profiler           Profiles benchmarked code using selected profiler. 
                           Currently the only available is "ETW" for Windows.

  -f, --filter             Glob patterns

  -i, --inProcess          (Default: false) Run benchmarks in Process

  -a, --artifacts          Valid path to accessible directory

  --outliers               (Default: RemoveUpper) 
                           DontRemove/RemoveUpper/RemoveLower/RemoveAll

  --affinity               Affinity mask to set for the benchmark process

  --allStats               (Default: false) Displays all statistics (min, max &
                           more)

  --allCategories          Categories to run. If few are provided, only the 
                           benchmarks which belong to all of them are going to 
                           be executed

  --anyCategories          Any Categories to run

  --attribute              Run all methods with given attribute (applied to 
                           class or method)

  --join                   (Default: false) Prints single table with results 
                           for all benchmarks

  --keepFiles              (Default: false) Determines if all auto-generated 
                           files should be kept or removed after running the 
                           benchmarks.

  --noOverwrite            (Default: false) Determines if the exported result 
                           files should not be overwritten (be default they are
                           overwritten).

  --counters               Hardware Counters

  --cli                    Path to dotnet cli (optional).

  --packages               The directory to restore packages to (optional).

  --coreRun                Path(s) to CoreRun (optional).

  --monoPath               Optional path to Mono which should be used for 
                           running benchmarks.

  --clrVersion             Optional version of private CLR build used as the 
                           value of COMPLUS_Version env var.

  --coreRtVersion          Optional version of Microsoft.DotNet.ILCompiler 
                           which should be used to run with CoreRT. Example: 
                           "1.0.0-alpha-26414-01"

  --ilcPath                Optional IlcPath which should be used to run with 
                           private CoreRT build.

  --launchCount            How many times we should launch process with target 
                           benchmark. The default is 1.

  --warmupCount            How many warmup iterations should be performed. If 
                           you set it, the minWarmupCount and maxWarmupCount 
                           are ignored. By default calculated by the heuristic.

  --minWarmupCount         Minimum count of warmup iterations that should be 
                           performed. The default is 6.

  --maxWarmupCount         Maximum count of warmup iterations that should be 
                           performed. The default is 50.

  --iterationTime          Desired time of execution of an iteration in 
                           milliseconds. Used by Pilot stage to estimate the 
                           number of invocations per iteration. 500ms by default

  --iterationCount         How many target iterations should be performed. By 
                           default calculated by the heuristic.

  --minIterationCount      Minimum number of iterations to run. The default is 
                           15.

  --maxIterationCount      Maximum number of iterations to run. The default is 
                           100.

  --invocationCount        Invocation count in a single iteration. By default 
                           calculated by the heuristic.

  --unrollFactor           How many times the benchmark method will be invoked 
                           per one iteration of a generated loop. 16 by default

  --strategy               The RunStrategy that should be used. 
                           Throughput/ColdStart/Monitoring.

  --runOncePerIteration    (Default: false) Run the benchmark exactly once per 
                           iteration.

  --info                   (Default: false) Print environment information.

  --list                   (Default: Disabled) Prints all of the available 
                           benchmark names. Flat/Tree

  --disasmDepth            (Default: 1) Sets the recursive depth for the 
                           disassembler.

  --disasmDiff             (Default: false) Generates diff reports for the 
                           disassembler.

  --buildTimeout           Build timeout in seconds.

  --stopOnFirstError       (Default: false) Stop on first error.

  --statisticalTest        Threshold for Mann–Whitney U Test. Examples: 5%, 
                           10ms, 100ns, 1s

  --disableLogFile         Disables the logfile.

  --maxWidth               Max paramter column width, the default is 20.

  --help                   Display this help screen.

  --version                Display version information.
```

## Read more

* [.NET Core Global Tools overview](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)