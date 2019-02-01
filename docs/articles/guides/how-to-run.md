# How to run your benchmarks

There are several ways to run your benchmarks:

## Types

```cs
var summary = BenchmarkRunner.Run<MyBenchmarkClass>();
var summary = BenchmarkRunner.Run(typeof(MyBenchmarkClass));
```

## Global dotnet tool

You can also run a banchmark using `BenchmarkDotNet.Tool`.

```log
dotnet tool install -g BenchmarkDotNet.Tool
```

```log
dotnet benchmark MyAssemblyWithBenchmarks.dll --filter *
```

## Url

You can also run a benchmark directly from the internet:

```cs
string url = "<E.g. direct link to raw content of a gist>";
var summary = BenchmarkRunner.RunUrl(url);
```

## Source

```cs
string benchmarkSource = "public class MyBenchmarkClass { ...";
var summary = BenchmarkRunner.RunSource(benchmarkSource);
```

## BenchmarkSwitcher

Or you can create a set of benchmarks and choose one from command line:

```cs
static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

Also you can use the config command style to specify some config from command line (more @docs.console-args):

```log
dotnet run -c Release -- --job short --runtimes clr core --filter *BenchmarkClass1*
```

