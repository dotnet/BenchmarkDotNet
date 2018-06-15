---
uid: docs.diagnosers
name: Diagnosers
---

# Diagnosers

A **diagnoser** can attach to your benchmark and get some useful info.

The current Diagnosers are:

- GC and Memory Allocation (`MemoryDiagnoser`) which is cross platform, built-in and **is not enabled by default anymore**.
  Please see Adam Sitnik's [blog post](http://adamsitnik.com/the-new-Memory-Diagnoser/) for all the details.
- JIT Inlining Events (`InliningDiagnoser`).
  You can find this diagnoser in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`):
  [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/)
- JIT Tail Call Events (`TailCallDiagnoser`).
  You can find this diagnoser as well as the (`InliningDiagnoser`) in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`):
  [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/) Please see [this post](https://georgeplotnikov.github.io/articles/tale-tail-call-dotnet) for all the details.
- Hardware Counter Diagnoser.
  You can find this diagnoser in a separated package with diagnosers for Windows (`BenchmarkDotNet.Diagnostics.Windows`):
  [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/).
  Please see Adam Sitnik's [blog post](http://adamsitnik.com/Hardware-Counters-Diagnoser/) for all the details.
- Disassembly Diagnoser.
  It allows you to disassemble the benchmarked code to asm, IL and C#/F#.
  Please see Adam Sitnik's [blog post](http://adamsitnik.com/Disassembly-Diagnoser/) for all the details.

## Usage

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
[TailCallDiagnoser]
```

In BenchmarkDotNet, 1kB = 1024B, 1MB = 1024kB, and so on.

## Restrictions

* In order to not affect main results we perform a separate run if any diagnoser is used. That's why it might take more time to execute benchmarks.
* MemoryDiagnoser:
	* Mono currently [does not](http://stackoverflow.com/questions/40234948/how-to-get-the-number-of-allocated-bytes-in-mono) expose any api to get the number of allocated bytes. That's why our Mono users will get `?` in Allocated column.
	* In order to get the number of allocated bytes in cross platform way we are using `GC.GetAllocatedBytesForCurrentThread` which recently got [exposed](https://github.com/dotnet/corefx/pull/12489) for netcoreapp1.1. That's why BenchmarkDotNet does not support netcoreapp1.0 from version 0.10.1.
	* MemoryDiagnoser is `99.5%` accurate about allocated memory when using default settings or Job.ShortRun (or any longer job than it).
* HardwareCounters:
	* Windows 8+ only (we plan to add Unix support in the future)
    * No Hyper-V (Virtualization) support
    * Requires running as Admin (ETW Kernel Session)
    * No `InProcessToolchain` support ([#394](https://github.com/dotnet/BenchmarkDotNet/issues/394))
* Disassembly Diagnoser:
    * .NET Core disassembler works only on Windows
    * Mono disassembler does not support recursive disassembling and produces output without IL and C#.
    * Indirect calls are not tracked.
    * To be able to compare different platforms, you need to target AnyCPU `<PlatformTarget>AnyCPU</PlatformTarget>`
    * To get the corresponding C#/F# code from disassembler you need to configure your project in following way:

```xml
<DebugType>pdbonly</DebugType>
<DebugSymbols>true</DebugSymbols>
```

---

[!include[IntroHardwareCounters](../samples/IntroHardwareCounters.md)]

[!include[IntroDisassemblyRyuJit](../samples/IntroDisassemblyRyuJit.md)]

[!include[IntroDisassembly](../samples/IntroDisassembly.md)]

[!include[IntroDisassemblyAllJits](../samples/IntroDisassemblyAllJits.md)]

[!include[IntroDisassemblyDry](../samples/IntroDisassemblyDry.md)]

[!include[IntroTailcall](../samples/IntroTailcall.md)]