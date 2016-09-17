#Welcome to the BenchmarkDotNet documentation

**BenchmarkDotNet** is a powerful .NET library for benchmarking. 

Source code is available at [@fa-github github.com/PerfDotNet/BenchmarkDotNet](https://github.com/PerfDotNet/BenchmarkDotNet)

[![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) [![Gitter](https://img.shields.io/gitter/room/PerfDotNet/BenchmarkDotNet.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet) [![Build status](https://img.shields.io/appveyor/ci/perfdotnet/benchmarkdotnet/master.svg?label=appveyor)](https://ci.appveyor.com/project/PerfDotNet/benchmarkdotnet/branch/master) [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md) [![Overview](https://img.shields.io/badge/docs-Overview-green.svg?style=flat)](https://perfdotnet.github.io/BenchmarkDotNet/Overview.htm) [![ChangeLog](https://img.shields.io/badge/docs-ChangeLog-green.svg?style=flat)](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/ChangeLog)

## Summary

* Standard benchmarking routine: generating an isolated project per each benchmark method; auto-selection of iteration amount; warmup; overhead evaluation; statistics calculation; and so on.
* Supported runtimes: Full .NET Framework, .NET Core (RTM), Mono
* Supported languages: C#, F#, and Visual Basic
* Supported OS: Windows, Linux, MacOS
* Easy way to compare different environments (`x86` vs `x64`, `LegacyJit` vs `RyuJit`, and so on; see: [Jobs](https://perfdotnet.github.io/BenchmarkDotNet/Configs/Jobs.htm))
* Reports: markdown, csv, html, plain text, png plots.
* Advanced features: [Baseline](https://perfdotnet.github.io/BenchmarkDotNet/Advanced/Baseline.htm), [Params](https://perfdotnet.github.io/BenchmarkDotNet/Advanced/Params.htm)
* Powerful diagnostics based on ETW events (see [BenchmarkDotNet.Diagnostics.Windows](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/))