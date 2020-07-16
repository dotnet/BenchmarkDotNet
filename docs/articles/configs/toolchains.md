---
uid: docs.toolchains
name: Toolchains
---

# Toolchains

To achieve process-level isolation, BenchmarkDotNet generates, builds and executes a new console app per every benchmark. A **toolchain** contains generator, builder, and executor.

When you run your benchmarks without specifying the toolchain in an explicit way, the default one is used:

* Roslyn for Full .NET Framework and Mono
* dotnet cli for .NET Core and CoreRT

## Multiple frameworks support


If you want to test multiple frameworks, your project file **MUST target all of them** and you **MUST install the corresponding SDKs**:

```xml
<TargetFrameworks>netcoreapp3.0;netcoreapp2.1;net48</TargetFrameworks>
```

If you run your benchmarks without specifying any custom settings, BenchmarkDotNet is going to run the benchmarks **using the same framework as the host process**:

```cmd
dotnet run -c Release -f netcoreapp2.1 # is going to run the benchmarks using .NET Core 2.1
dotnet run -c Release -f netcoreapp3.0 # is going to run the benchmarks using .NET Core 3.0
dotnet run -c Release -f net48         # is going to run the benchmarks using .NET 4.8
mono $pathToExe                        # is going to run the benchmarks using Mono from your PATH
```

To run the benchmarks for multiple runtimes with a single command, you need to specify the target framework moniker names via `--runtimes|-r` console argument:

```cmd
dotnet run -c Release -f netcoreapp2.1 --runtimes netcoreapp2.1 netcoreapp3.0 # is going to run the benchmarks using .NET Core 2.1 and .NET Core 3.0
dotnet run -c Release -f netcoreapp2.1 --runtimes netcoreapp2.1 net48         # is going to run the benchmarks using .NET Core 2.1 and .NET 4.8
```

What is going to happen if you provide multiple Full .NET Framework monikers? Let's say:

```cmd
dotnet run -c Release -f net461 net472 net48
```

Full .NET Framework always runs every .NET executable using the latest .NET Framework available on a given machine. If you try to run the benchmarks for a few .NET TFMs, they are all going to be executed using the latest .NET Framework from your machine. The only difference is that they are all going to have different features enabled depending on target version they were compiled for. You can read more about this [here](https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/version-compatibility) and [here](https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/application-compatibility). This is **.NET Framework behavior which can not be controlled by BenchmarkDotNet or any other tool**.

**Note:** Console arguments support works only if you pass the `args` to `BenchmarkSwitcher`:

```cs
class Program
{
    static void Main(string[] args) 
        => BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args); // crucial to make it work
}
```

You can achieve the same thing using `[SimpleJobAttribute]`:

```cs
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Mono)]
    [SimpleJob(RuntimeMoniker.NetCoreApp21)]
    [SimpleJob(RuntimeMoniker.NetCoreApp30)]
    public class TheClassWithBenchmarks
```

Or using a custom config:

```cs
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = DefaultConfig.Instance
                .With(Job.Default.With(CoreRuntime.Core21))
                .With(Job.Default.With(CoreRuntime.Core30))
                .With(Job.Default.With(ClrRuntime.Net48))
                .With(Job.Default.With(MonoRuntime.Default));

            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, config);
        }
    }
}
```

The recommended way of running the benchmarks for multiple runtimes is to use the `--runtimes` console line argument. By using the console line argument you don't need to edit the source code anytime you want to change the list of runtimes. Moreover, if you share the source code of the benchmark other people can run it even if they don't have the exact same framework version installed.


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
            Job.ShortRun.With(ClrRuntime.CreateForLocalFullNetFrameworkBuild(version: "4.0"))));
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
  *  you have to specify the CoreRT runtime in an explicit way, either by using `[SimpleJob]` attribute or by using the fluent Job config API `Job.ShortRun.With(CoreRtRuntime.$version)`
  *  to run CoreRT benchmark you run the app as a .NET Core/.NET process (`dotnet run -c Release -f netcoreapp2.1`) and BenchmarkDotNet does all the CoreRT compilation for you. If you want to check what files are generated you need to apply `[KeepBenchmarkFiles]` attribute to the class which defines benchmarks.

By default BenchmarkDotNet uses the latest version of `Microsoft.DotNet.ILCompiler` to build the CoreRT benchmark according to [this instructions](https://github.com/dotnet/corert/tree/7f902d4d8b1c3280e60f5e06c71951a60da173fb/samples/HelloWorld#add-corert-to-your-project).

```cs
var config = DefaultConfig.Instance
    .With(Job.Default.With(CoreRtRuntime.CoreRt21)); // compiles the benchmarks as netcoreapp2.1 and uses the latest CoreRT to build a native app

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);
```

```cs
[SimpleJob(RuntimeMoniker.CoreRt21)] // compiles the benchmarks as netcoreapp2.1 and uses the latest CoreRT to build a native app
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
        .With(CoreRtToolchain.CreateBuilder()
            .UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-26412-02") // the version goes here
            .DisplayName("CoreRT NuGet")
            .TargetFrameworkMoniker("netcoreapp2.1")
            .ToToolchain()));
```

### Compiling source to native code using the ILCompiler you built

If you are an CoreRT contributor and you want to benchmark your local build of CoreRT you have to provide necessary info (IlcPath):

```cs
var config = DefaultConfig.Instance
    .With(Job.ShortRun
        .With(CoreRtToolchain.CreateBuilder()
            .UseCoreRtLocal(@"C:\Projects\corert\bin\Windows_NT.x64.Release") // IlcPath
            .DisplayName("Core RT RyuJit")
            .TargetFrameworkMoniker("netcoreapp2.1")
            .ToToolchain()));
```

BenchmarkDotNet is going to follow [these instructrions](https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built) to get it working for you.

### Using CPP Code Generator

> This approach uses transpiler to convert IL to C++, and then uses platform specific C++ compiler and linker for compiling/linking the application. The transpiler is a lot less mature than the RyuJIT path. If you came here to give CoreRT a try on your .NET Core program, use the RyuJIT option above.

If you want to test [CPP Code Generator](https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#using-cpp-code-generator) you have to use `UseCppCodeGenerator` method:

```cs
var config = DefaultConfig.Instance
    .With(Job.Default
        .With(
            CoreRtToolchain.CreateBuilder()
                .UseCoreRtLocal(@"C:\Projects\corert\bin\Windows_NT.x64.Release") // IlcPath
                .UseCppCodeGenerator() // ENABLE IT
                .TargetFrameworkMoniker("netcoreapp2.1")
                .DisplayName("CPP")
                .ToToolchain()));
```

**Note**: You might get some `The method or operation is not implemented.` errors as of today if the code that you are trying to benchmark is using some features that are not implemented by CoreRT/transpiler yet...

## Wasm

BenchmarkDotNet supports Web Assembly on Unix! However, currently you need to build the **dotnet runtime** yourself to be able to run the benchmarks.

For up-to-date docs, you should visit [dotnet/runtime repository](https://github.com/dotnet/runtime/blob/master/docs/workflow/testing/libraries/testing-wasm.md).

The docs below are specific to Ubuntu 18.04 at the moment of writing this document (16/07/2020).

Firs of all, you need to install.... **npm** 10+:

```cmd
curl -sL https://deb.nodesource.com/setup_12.x | sudo -E bash -
sudo apt install nodejs
```

After this, you need to install [jsvu](https://github.com/GoogleChromeLabs/jsvu):

```cmd
npm install jsvu -g
```

Add it to PATH:

```cmd
export PATH="${HOME}/.jsvu:${PATH}"
```

And use it to install V8, JavaScriptCore and SpiderMonkey:

```cmd
jsvu --os=linux64 --engines=javascriptcore,spidermonkey,v8
```

Now you need to install [Emscripten](https://emscripten.org/docs/getting_started/downloads.html#installation-instructions):

```cmd
git clone https://github.com/emscripten-core/emsdk.git
cd emsdk
./emsdk install latest
./emsdk activate latest
source ./emsdk_env.sh
```

The last thing before cloning dotnet/runtime repository is creation of `EMSDK_PATH` env var used by Mono build scripts:

```cmd
export EMSDK_PATH=$EMSDK
```

Now you need to clone dotnet/runtime repository:

```cmd
git clone https://github.com/dotnet/runtime
cd runtime
```

Install [all Mono prerequisites](https://github.com/dotnet/runtime/blob/master/docs/workflow/testing/libraries/testing-wasm.md):

```cmd
sudo apt-get install cmake llvm-9 clang-9 autoconf automake libtool build-essential python curl git lldb-6.0 liblldb-6.0-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libssl-dev libnuma-dev libkrb5-dev zlib1g-dev
```

And FINALLY build Mono Runtime with Web Assembly support:

```cmd
./build.sh --arch wasm --os Browser -c release
```

Before you run the benchmarks, you need to make sure that following two file exists:

```cmd
runtime/src/mono/wasm/runtime-test.js
runtime/build.sh
```

And that you have .NET 5 feed added to your `nuget.config` file:

```xml
<add key="dotnet5" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json" />
```

Now you should be able to run the Wasm benchmarks!

[!include[IntroWasm](../samples/IntroWasm.md)]

