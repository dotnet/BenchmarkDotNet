#Welcome to the BenchmarkDotNet documentation

<img src="logo/logo-wide.png" width="600px" />

[![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) [![Gitter](https://img.shields.io/gitter/room/dotnet/BenchmarkDotNet.svg)](https://gitter.im/dotnet/BenchmarkDotNet) [![Build status](https://img.shields.io/appveyor/ci/dotnetfoundation/benchmarkdotnet/master.svg?label=appveyor)](https://ci.appveyor.com/project/dotnetfoundation/benchmarkdotnet/branch/master) [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md) [![Overview](https://img.shields.io/badge/docs-Overview-green.svg?style=flat)](http://benchmarkdotnet.org/Overview.htm) [![ChangeLog](https://img.shields.io/badge/docs-ChangeLog-green.svg?style=flat)](https://github.com/dotnet/BenchmarkDotNet/wiki/ChangeLog)

**BenchmarkDotNet** is a powerful .NET library for benchmarking.

Source code is available at [@fa-github github.com/dotnet/BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)

Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
BenchmarkDotNet will protect you from the common pitfalls (even for experienced developers) because it does all the dirty work for you:
  it generates an isolated project per each benchmark method,
  does several launches of this project,
  run multiple iterations of the method (include warm-up), and so on.
Usually, you even shouldn't care about a number of iterations because BenchmarkDotNet chooses it automatically to achieve the requested level of precision.

It's really easy to design a performance experiment with BenchmarkDotNet.
Just mark your method with the [Benchmark] attribute and the benchmark is ready.
Want to run your code on CoreCLR, Mono, and the Full .NET Framework?
No problem: a few more attributes and the corresponded projects will be generated; the results will be presented at the same summary table.
In fact, you can compare any environment that you want:
  you can check performance difference between processor architectures (x86/x64),
  JIT versions (LegacyJIT/RyuJIT),
  different sets of GC flags (like Server/Workstation),
  and so on.
You can also introduce one or several parameters and check the performance on different inputs at once.

BenchmarkDotNet helps you not only run benchmarks but also analyze the results: it generates reports in different formats and renders nice plots.
It calculates many statistics, allows you to run statistical tests, and compares results of different benchmark methods.
So it doesn't overload you with data, by default BenchmarkDotNet prints only the really important statistical values depending on your results:
  it allows you to keep summary small and simple for primitive cases but notify you about additional important area for complicated cases
  (of course you can request any numbers manually via additional attributes).

## Summary

* Standard benchmarking routine: generating an isolated project per each benchmark method; auto-selection of iteration amount; warmup; overhead evaluation; statistics calculation; and so on.
* Supported runtimes: Full .NET Framework (4.6+), .NET Core (1.1+), Mono
* Supported languages: C#, F#, and Visual Basic
* Supported OS: Windows, Linux, MacOS
* Easy way to compare different environments (`x86` vs `x64`, `LegacyJit` vs `RyuJit`, and so on; see: [Jobs](http://benchmarkdotnet.org/Configs/Jobs.htm))
* Reports: markdown, csv, html, json, xml, plain text and png plots
* Advanced features: [Baseline](http://benchmarkdotnet.org/Advanced/Baseline.htm), [Params](http://benchmarkdotnet.org/Advanced/Params.htm)
* Powerful diagnostics based on ETW events (see [BenchmarkDotNet.Diagnostics.Windows](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/))

## Code of Conduct

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). 

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).