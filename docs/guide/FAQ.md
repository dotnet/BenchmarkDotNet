# FAQ (Frequently asked questions)

* **Q** Why I can't install BenchmarkDotNet in Visual Studio 2010/2012/2013?  
 **A**
BenchmarkDotNet requires NuGet 3.x+ and can't be installed in old versions of Visual Studio which use NuGet 2.x.
Consider to use Visual Studio 2015/2017 or [Rider](http://jetbrains.com/rider/).
See also: [BenchmarkDotNet#237](https://github.com/dotnet/BenchmarkDotNet/issues/237), [roslyn#12780](https://github.com/dotnet/roslyn/issues/12780).

* **Q** Why I can't install BenchmarkDotNet in a new .NET Core Console App in Visual Studio 2017?  
**A** BenchmarkDotNet supports only netcoreapp1.1+.
By default, Visual Studio 2017 creates a new application which targets netcoreapp1.0.
You should upgrade it up to 1.1.
If your want to target netcoreapp1.0 in your main assembly, it's recommended to create a separated project for benchmarks.

* **Q** I created a new .NET Core Console App in Visual Studio 2017. Now I want to run my code on CoreCLR, full .NET Framework, and Mono. How can I do it?  
**A** Use the following lines in your `.csproj` file:
```xml
<TargetFrameworks>netcoreapp1.1;net46</TargetFrameworks>
<RuntimeIdentifier Condition=" '$(TargetFramework)' == 'net46' ">win7-x86</RuntimeIdentifier>
<PlatformTarget>AnyCPU</PlatformTarget>
```
And mark your benchmark class with the following attributes:
```cs
[CoreJob, ClrJob, MonoJob]
```