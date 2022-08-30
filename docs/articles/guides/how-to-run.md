---
uid: docs.how-to-run
---

# How to run your benchmarks

There are several ways to run your benchmarks. What is important is that **BenchmarkDotNet works only with Console Apps**. It does not support any other kind of application like ASP.NET, Azure WebJobs, etc.

## Types

If you have just a few types with benchmarks, you can use `BenchmarkRunner`:

```cs
var summary = BenchmarkRunner.Run<MyBenchmarkClass>();
var summary = BenchmarkRunner.Run(typeof(MyBenchmarkClass));
```

The disadvantage of `BenchmarkRunner` is that it always runs all benchmarks in a given type (or assembly) and to change the type you need to modify the source code. But it's great for a quick start.

## BenchmarkSwitcher

If you have more types and you want to choose which benchmark to run (either by using console line arguments or console input) you should use `BenchmarkSwitcher`:

```cs
static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

Also you can use the config command style to specify some config from command line (more [](xref:docs.console-args)):

```log
dotnet run -c Release -- --job short --runtimes net472 net7.0 --filter *BenchmarkClass1*
```

The most important thing about `BenchmarkSwitcher` is that you need to pass the `args` from `Main` to the `Run` method. If you don't, it won't parse the arguments.


## Url

You can also run a benchmark directly from the internet:

```cs
string url = "<E.g. direct link to raw content of a gist>";
var summary = BenchmarkRunner.RunUrl(url);
```

**Note:** it works only for Full .NET Framework. It's not recommended to use this approach.

## Source

```cs
string benchmarkSource = "public class MyBenchmarkClass { ...";
var summary = BenchmarkRunner.RunSource(benchmarkSource);
```

**Note:** it works only for Full .NET Framework. It's not recommended to use this approach.
