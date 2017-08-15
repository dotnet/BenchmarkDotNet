# FAQ (Frequently asked questions)

* **Q** Why can't I install BenchmarkDotNet in Visual Studio 2010/2012/2013?  
 **A**
BenchmarkDotNet requires NuGet 3.x+ and can't be installed in old versions of Visual Studio which use NuGet 2.x.
Consider to use Visual Studio 2015/2017 or [Rider](http://jetbrains.com/rider/).
See also: [BenchmarkDotNet#237](https://github.com/dotnet/BenchmarkDotNet/issues/237), [roslyn#12780](https://github.com/dotnet/roslyn/issues/12780).

* **Q** Why can't I install BenchmarkDotNet in a new .NET Core Console App in Visual Studio 2017?  
**A** BenchmarkDotNet supports only netcoreapp1.1+.
By default, Visual Studio 2017 creates a new application which targets netcoreapp1.0.
You should upgrade it up to 1.1.
If you want to target netcoreapp1.0 in your main assembly, it's recommended to create a separated project for benchmarks.

* **Q** I created a new .NET Core Console App in Visual Studio 2017. Now I want to run my code on CoreCLR, full .NET Framework, and Mono. How can I do it?  
**A** Use the following lines in your `.csproj` file:
```xml
<TargetFrameworks>netcoreapp1.1;net46</TargetFrameworks>
<PlatformTarget>AnyCPU</PlatformTarget>
```
And mark your benchmark class with the following attributes:
```cs
[CoreJob, ClrJob, MonoJob]
```

* **Q** My source code targets old versions of .NET Framework or .NET Core, but BenchmarkDotNet requires `net46` and `netcoreapp1.1`. How can I run benchmarks in this case?  
**A** It's a good practice to introduce an additional console application (e.g. `MyAwesomeLibrary.Benchmarks`) which will depend on your code and BenchmarkDotNet.
Due to the fact that users usually run benchmarks in a develop environment and don't distribute benchmarks for users, it shouldn't be a problem.

* **Q** I wrote a small benchmark, but BenchmarkDotNet requires a lot of time for time measurements. How can I reduce this time?  
**A** By default, BenchmarkDotNet automatically chooses a number of iterations which allows achieving the best precision.
If you don't need such level of precision and just want to have a quick way to get approximated results, you can specify all parameters manually.
For example, you can use the `SimpleJob` or `ShortRunJob` attributes:
```cs
[SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 5, invocationCount:100, id: "QuickJob")]
[ShortRunJob]
```
* **Q** My benchmark unexpectedly stopped and I saw the information about error code. What can I do?  
**A** BenchmarkDotNet generates, builds and runs new process for every benchmark. This behavior is sometimes interpreted by anti-virus as dangerous, and the process is killed. Use `EnvironmentAnalyser` to detect antivirus software and configure your benchmark to use `InProcessToolchain`.

* **Q** Can I run benchmark on the virtual machine?  
**A** Yes, of course. However, it can affect results because of the shared, physical machine, virtualization process and incorrect `Stopwatch.Frequency`. If you are unsure whether an application is running on virtual environment, use `EnvironmentAnalyser` to detect VM hypervisor.