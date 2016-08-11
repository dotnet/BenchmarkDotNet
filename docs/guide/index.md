#Welcome to the BenchmarkDotNet documentation

**BenchmarkDotNet** is a powerful .NET library for benchmarking. 

Source-code is available at: [@fa-github Github](https://github.com/PerfDotNet/BenchmarkDotNet)  
Pre-built packages are available at: [NuGet](https://www.nuget.org/packages/BenchmarkDotNet/)

## Status

* Main: [![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://img.shields.io/gitter/room/PerfDotNet/BenchmarkDotNet.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet) [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md) [![NuGet](https://img.shields.io/nuget/v/BenchmarkDotNet.svg)](https://www.nuget.org/packages/BenchmarkDotNet/) 
* Docs: [![ChangeLog](https://img.shields.io/badge/docs-ChangeLog-green.svg?style=flat)](https://github.com/PerfDotNet/BenchmarkDotNet/wiki/ChangeLog) [![Guide for developers](https://img.shields.io/badge/docs-GuideForDevelopers-green.svg?style=flat)](https://github.com/PerfDotNet/BenchmarkDotNet/blob/develop/DEVELOPING.md)
* Build: [![Build status: master](https://img.shields.io/appveyor/ci/perfdotnet/benchmarkdotnet/master.svg?label=master)](https://ci.appveyor.com/project/PerfDotNet/benchmarkdotnet/branch/master) [![Build status: develop](https://img.shields.io/appveyor/ci/perfdotnet/benchmarkdotnet/develop.svg?label=develop)](https://ci.appveyor.com/project/PerfDotNet/benchmarkdotnet/branch/develop)

## Summary

* Standard benchmarking routine: generating an isolated project per each benchmark method; auto-selection of iteration amount; warmup; overhead evaluation; statistics calculation; and so on.
* Easy way to compare different environments (`x86` vs `x64`, `LegacyJit` vs `RyuJit`, and so on; see: [Jobs](Configuration/Jobs.htm))
* Reports: markdown (default, github, stackoverflow), csv, html, plain text; png plots.
* Advanced features: [Baseline](Advanced/baseline.htm), [Setup](Advanced/setup.htm), [Params](Advanced/params.htm), [Percentiles](Advanced/percentiles.htm)
* Powerful diagnostics based on ETW events (see [BenchmarkDotNet.Diagnostics.Windows](https://www.nuget.org/packages/BenchmarkDotNet.Diagnostics.Windows/))
* Supported runtimes: Full .NET Framework, .NET Core (RTM), Mono
* Supported languages: C#, F# (also on [.NET Core](https://github.com/PerfDotNet/BenchmarkDotNet/issues/135)) and Visual Basic