---
uid: docs.dotnet-benchmarkdotnet
name: The global BenchmarkDotNet tool 
---

# BenchmarkDotNet as global dotnet tool  

BenchmarkDotNet is also available as a global dotnet tool and provides a convenient way to execute your benchmark(s) from the command line interface.

## How to install the tool

Download and install the [.NET Core 2.1 SDK](https://www.microsoft.com/net/download) or newer. Once installed, run the following command:

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

## Usage

The basic usage syntax is:

```log
dotnet benchmark [arguments] [options]
```

### Arguments

* The **first** argument in `[arguments]` **must** be the path to an assembly file with your benchmarks.
* Further arguments are passed to the the `BenchmarkSwitcher`.

### Options

| Option | Description |
| ------ | ----------- |
|--version|Show version information|
|-?, -h or --help|Show help information|

```log
dotnet benchmark -?
```

**Note**: This shows also all valid arguments for `BenchmarkSwitcher`.

## Examples

The following example scans the `MyAssemblyWithBenchmarks.dll` for benchmarks and lets you select which benchmark(s) to execute: 

```log
dotnet benchmark MyAssemblyWithBenchmarks.dll
```

To execute all benchmarks use `--filter *`:

```log
dotnet benchmark MyAssemblyWithBenchmarks.dll --filter *
```

**Note**: For further arguments for the `BenchmarkSwticher` see also [Console Arguments](console-args.md).
