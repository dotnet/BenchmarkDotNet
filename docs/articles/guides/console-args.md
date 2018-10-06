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

The `--runtimes` or just `-r` allows you to run the benchmarks for selected Runtimes. Available options are: Mono, CoreRT, net46, net461, net462, net47, net471, net472, netcoreapp2.0, netcoreapp2.1, netcoreapp2.2, netcoreapp3.0.

Example: run the benchmarks for .NET 4.7.2 and .NET Core 2.1:

```log
dotnet run -c Release -- --runtimes net472 netcoreapp2.1
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

