# Diagnosers

A **diagnoser** can attach to your benchmark and get some useful info. There is a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`):

[![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/)


The current Diagnosers are:

- GC and Memory Allocation (`MemoryDiagnoser`)
- JIT Inlining Events (`InliningDiagnoser`)


## Examples

Below is a sample output from the `GC and Memory Allocation` diagnoser, note the extra columns on the right-hand side ("Gen 0", "Gen 1", "Gen 2" and "Bytes Allocated/Op"):

    Method |  Lookup |     Median |    StdDev | Scaled |    Gen 0 | Gen 1 | Gen 2 | Bytes Allocated/Op |
---------- |-------- |----------- |---------- |------- |--------- |------ |------ |------------------- |
      LINQ | Testing | 49.1154 ns | 0.5301 ns |   2.48 | 1,526.00 |     - |     - |              25.21 |
 Iterative | Testing | 19.8040 ns | 0.0456 ns |   1.00 |        - |     - |     - |               0.00 |


A config example:

```cs
private class Config : ManualConfig
{
    public Config()
    {
        Add(new MemoryDiagnoser());
        Add(new InliningDiagnoser());
    }
}
```
