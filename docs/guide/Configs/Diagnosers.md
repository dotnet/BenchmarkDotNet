# Diagnosers

A **diagnoser** can attach to your benchmark and get some useful info.

The current Diagnosers are:

- GC and Memory Allocation (`MemoryDiagnoser`) which is cross platform, built-in and **is not enabled by default anymore**.
- JIT Inlining Events (`InliningDiagnoser`). You can find this diagnoser in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`): [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/)


## Examples

Below is a sample output from the `GC and Memory Allocation` diagnoser, note the extra columns on the right-hand side ("Gen 0", "Gen 1", "Gen 2" and "Allocated"):

```
           Method |        Mean |     StdErr |      StdDev |      Median |  Gen 0 | Allocated |
----------------- |------------ |----------- |------------ |------------ |------- |---------- |
 'new byte[10kB]' | 884.4896 ns | 46.3528 ns | 245.2762 ns | 776.4237 ns | 0.1183 |     10 kB |
 ```

A config example:

```cs
private class Config : ManualConfig
{
    public Config()
    {
        Add(MemoryDiagnoser.Default);
        Add(new InliningDiagnoser());
    }
}
```

You can also use one of the following attributes (apply it on a class that contains Benchmarks):
```cs
[MemoryDiagnoser]
[InliningDiagnoser]
```

## Restrictions

* Mono currently [does not](http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono) expose any api to get the number of allocated bytes. That's why our Mono users will get `?` in Allocated column.
* In order to get the number of allocated bytes in cross platform way we are using `GC.GetAllocatedBytesForCurrentThread` which recently got [exposed](https://github.com/dotnet/corefx/pull/12489) for netcoreapp1.1. That's why BenchmarkDotNet does not support netcoreapp1.0 from version 0.10.1.
* In order to not affect main results we perform a separate run if any diagnoser is used. That's why it might take more time to execute benchmarks.
* MemoryDiagnoser is `100%` accurate about allocated memory when using Job.ShortRun or longer job.
