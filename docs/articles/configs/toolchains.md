---
uid: docs.toolchains
name: Toolchains
---

# Toolchains

In BenchmarkDotNet we generate, build and execute new console app per every benchmark. A **toolchain** contains generator, builder, and executor. 

When you run your benchmarks without specifying the toolchain in an explicit way, we use the default one. It works OOTB, you don't need to worry about anything.

We use Roslyn for classic .NET and Mono, and `dotnet cli` for .NET Core and CoreRT.

## Multiple frameworks support

You can target multiple frameworks with single, modern csproj file:

```xml
<TargetFrameworks>netcoreapp2.0;net462</TargetFrameworks>
```

BenchmarkDotNet allows you to take full advantage of that. With single config, we can execute the benchmarks for all the frameworks that you have listed in your csproj file.

If you specify `Runtime` in explicit way, we just choose the right toolchain for you.

```cs
[ClrJob, MonoJob, CoreJob, CoreRtJob]
public class Algo_Md5VsSha256
{
    // the benchmarks are going to be executed for classic .NET, Mono (default path), .NET Core and CoreRT (latest version)
}
```

### TFM

At some point of time we need to choose the target framework moniker (TFM).

When you are running your app with benchmark as .NET Core app, we just check the version of the `System.Runtime.dll` which allows us to decide which version of .NET Core you are using.

But when you are running your project as classic .NET (.NET 4.6.2 for example), we don't know which TFM to choose for your .NET Core Runtime, so we use the default one - **netcoreapp2.0**.

If the default `netcoreapp2.0` is not OK for you, you must configure the toolchains in explicit way:

```cs
public class MultipleRuntimes : ManualConfig
{
    public MultipleRuntimes()
    {
        Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp21)); // .NET Core 2.1

        Add(Job.Default.With(CsProjClassicNetToolchain.Net462)); // NET 4.6.2
    }
}

[Config(typeof(MultipleRuntimes))]
public class TypeWithBenchmarks
{
}
```

After doing this, you can run your benchmarks via:

* `dotnet run -c Release -f net462`
* `dotnet run -c Release -f netcoreapp2.0`

And they are going to be executed for both runtimes.

## Custom .NET Core Runtime

We can run your benchmarks for custom `<RuntimeFrameworkVersion>` if you want. All you need to do is to create custom toolchain by calling `CsProjCoreToolchain.From` method, which accepts `NetCoreAppSettings`.

```cs
public class MyConfig : ManualConfig
{
    public MyConfig()
    {
        Add(Job.Default.With(
            CsProjCoreToolchain.From(
                new NetCoreAppSettings(
                    targetFrameworkMoniker: "netcoreapp2.1", 
                    runtimeFrameworkVersion: "2.1.0-preview2-25628-01", 
                    name: ".NET Core 2.1"))));
    }
}
```

## Custom .NET Runtime

It's possible to benchmark a private build of .NET Runtime. All you need to do is to define a job with the right version of `ClrRuntime`.

```cs
BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, 
        DefaultConfig.Instance.With(
            Job.ShortRun.With(new ClrRuntime(version: "4.0"))));
```

This sends the provided version as a `COMPLUS_Version` env var to the benchmarked process.

## Custom dotnet cli path

We internally use dotnet cli to build and run .NET Core executables. Sometimes it might be mandatory to use non-default dotnet cli path. An example scenario could be a comparison of RyuJit 32bit vs 64 bit. It required due this [limitation](https://github.com/dotnet/cli/issues/7532) of dotnet cli

```cs
public class CustomPathsConfig : ManualConfig
{
    public CustomPathsConfig() 
    {
        var dotnetCli32bit = NetCoreAppSettings
            .NetCoreApp20
            .WithCustomDotNetCliPath(@"C:\Program Files (x86)\dotnet\dotnet.exe", "32 bit cli");

        var dotnetCli64bit = NetCoreAppSettings
            .NetCoreApp20
            .WithCustomDotNetCliPath(@"C:\Program Files\dotnet\dotnet.exe", "64 bit cli");

        Add(Job.RyuJitX86.With(CsProjCoreToolchain.From(dotnetCli32bit)).WithId("32 bit cli"));
        Add(Job.RyuJitX64.With(CsProjCoreToolchain.From(dotnetCli64bit)).WithId("64 bit cli"));
    }
}
```

``` ini
BenchmarkDotNet=v0.10.9.20170910-develop, OS=Windows 10 Redstone 1 (10.0.14393)
Processor=Intel Core i7-6600U CPU 2.60GHz (Skylake), ProcessorCount=4
Frequency=2742185 Hz, Resolution=364.6727 ns, Timer=TSC
.NET Core SDK=2.1.0-preview1-007074
  [Host]     : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  32 bit cli : .NET Core 2.0.0 (Framework 4.6.00001.0), 32bit RyuJIT
  64 bit cli : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Jit=RyuJit  
```

This feature is now also exposed with the `--cli` console argument.

Example: `dotnet run -c Release -- --cli "C:\Projects\machinelearning\Tools\dotnetcli\dotnet.exe"`

## CoreRun

To use CoreRun for running the benchmarks you need to use `--coreRun `command line argument. You can combine it with `--cli` described above. This is most probably the easiest and most reliable way of running benchmarks against local CoreFX/CoreCLR builds.

Example: `dotnet run -c Release -- --coreRun "C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe"`

---

[!include[IntroInProcess](../samples/IntroInProcess.md)]

[!include[IntroInProcessWrongEnv](../samples/IntroInProcessWrongEnv.md)]


## CoreRT

BenchmarkDotNet supports [CoreRT](https://github.com/dotnet/corert)! However, you might want to know how it works to get a better understanding of the results that you get.

* CoreRT is a flavor of .NET Core. Which means that:
  *  you have to target .NET Core to be able to build CoreRT benchmarks (`<TargetFramework>netcoreapp2.1</TargetFramework>` in the .csproj file)
  *  you have to specify the CoreRT runtime in an explicit way, either by using `[CoreRtJob]` attribute or by using the fluent Job config API `Job.ShortRun.With(Runtime.CoreRT)`
  *  to run CoreRT benchmark you run the app as a .NET Core/.NET process (`dotnet run -c Release -f netcoreapp2.1`) and BenchmarkDotNet does all the CoreRT compilation for you. If you want to check what files are generated you need to apply `[KeepBenchmarkFiles]` attribute to the class which defines benchmarks.

By default BenchmarkDotNet uses the latest version of `Microsoft.DotNet.ILCompiler` to build the CoreRT benchmark according to [this instructions](https://github.com/dotnet/corert/tree/7f902d4d8b1c3280e60f5e06c71951a60da173fb/samples/HelloWorld#add-corert-to-your-project).

```cs
var config = DefaultConfig.Instance
    .With(Job.Default.With(Runtime.CoreRT)); // uses the latest CoreRT version

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);
```

```cs
[CoreRtJob] // uses the latest CoreRT version
public class TheTypeWithBenchmarks
{
   [Benchmark] // the benchmarks go here
}
```

**Note**: BenchmarkDotNet is going to run `dotnet restore` on the auto-generated project. The first time it does so, it's going to take a **LOT** of time to download all the dependencies (few minutes). Just give it some time and don't press `Ctrl+C` too fast ;)

If you want to benchmark some particular version of CoreRT you have to specify it in an explicit way:

```cs
var config = DefaultConfig.Instance
    .With(Job.ShortRun
        .With(Runtime.CoreRT)
        .With(CoreRtToolchain.CreateBuilder()
            .UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-26412-02") // the version goes here
            .DisplayName("CoreRT NuGet")
            .ToToolchain()));
```

### Compiling source to native code using the ILCompiler you built

If you are an CoreRT contributor and you want to benchmark your local build of CoreRT you have to provide necessary info (IlcPath):

```cs
var config = DefaultConfig.Instance
    .With(Job.ShortRun
        .With(Runtime.CoreRT)
        .With(CoreRtToolchain.CreateBuilder()
            .UseCoreRtLocal(@"C:\Projects\corert\bin\Windows_NT.x64.Release") // IlcPath
            .DisplayName("Core RT RyuJit")
            .ToToolchain()));
```

BenchmarkDotNet is going to follow [these instructrions](https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built) to get it working for you.

### Using CPP Code Generator

> This approach uses transpiler to convert IL to C++, and then uses platform specific C++ compiler and linker for compiling/linking the application. The transpiler is a lot less mature than the RyuJIT path. If you came here to give CoreRT a try on your .NET Core program, use the RyuJIT option above.

If you want to test [CPP Code Generator](https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#using-cpp-code-generator) you have to use `UseCppCodeGenerator` method:

```cs
var config = DefaultConfig.Instance
    .With(Job.CoreRT.With(
        CoreRtToolchain.CreateBuilder()
            .UseCoreRtLocal(@"C:\Projects\corert\bin\Windows_NT.x64.Release") // IlcPath
            .UseCppCodeGenerator() // ENABLE IT
            .DisplayName("CPP")
            .ToToolchain()));
```

**Note**: You might get some `The method or operation is not implemented.` errors as of today if the code that you are trying to benchmark is using some features that are not implemented by CoreRT/transpiler yet...