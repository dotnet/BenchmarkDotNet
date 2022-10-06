---
uid: docs.toolchains
name: Toolchains
---

# Toolchains

To achieve process-level isolation, BenchmarkDotNet generates, builds and executes a new console app per every benchmark. A **toolchain** contains generator, builder, and executor.

When you run your benchmarks without specifying the toolchain in an explicit way, the default one is used:

* Roslyn for Full .NET Framework and Mono
* dotnet cli for .NET Core and NativeAOT

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


## NativeAOT

BenchmarkDotNet supports [NativeAOT](https://github.com/dotnet/runtime/tree/main/src/coreclr/nativeaot)! However, you might want to know how it works to get a better understanding of the results that you get.

As every AOT solution, NativeAOT has some [limitations](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/limitations.md) like limited reflection support or lack of dynamic assembly loading. Because of that, the host process (what you run from command line) is never an AOT process, but just a regular .NET process. This process (called Host process) uses reflection to read benchmarks metadata (find all `[Benchmark]` methods etc), generates a new project that references the benchmarks and compiles it using ILCompiler. Such compilation produces a native executable, which is later started by the Host process. This process (called Benchmark or Child process) performs the actual benchmarking and reports the results back to the Host process. By default BenchmarkDotNet uses the latest version of `Microsoft.DotNet.ILCompiler` to build the NativeAOT benchmark according to [this instructions](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/compiling.md).

This is why you need to:
- install [pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/deploying/native-aot/#prerequisites) required by NativeAOT compiler
- target .NET to be able to run NativeAOT benchmarks (example: `<TargetFramework>net7.0</TargetFramework>` in the .csproj file)
- run the app as a .NET process (example: `dotnet run -c Release -f net7.0`).
- specify the NativeAOT runtime in an explicit way, either by using console line arguments `--runtimes nativeaot7.0` (the recommended approach), or by using`[SimpleJob]` attribute or by using the fluent Job config API `Job.ShortRun.With(NativeAotRuntime.Net70)`:

```cmd
dotnet run -c Release -f net7.0 --runtimes nativeaot7.0
```

or:

```cs
var config = DefaultConfig.Instance
    .With(Job.Default.With(NativeAotRuntime.Net70)); // compiles the benchmarks as net7.0 and uses the latest NativeAOT to build a native app

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);
```

or:

```cs
[SimpleJob(RuntimeMoniker.NativeAot70)] // compiles the benchmarks as net7.0 and uses the latest NativeAOT to build a native app
public class TheTypeWithBenchmarks
{
   [Benchmark] // the benchmarks go here
}
```

### Customization

If you want to benchmark some particular version of NativeAOT (or from a different NuGet feed) you have to specify it in an explicit way:

```cs
var config = DefaultConfig.Instance
    .With(Job.ShortRun
        .With(NativeAotToolchain.CreateBuilder()
            .UseNuGet(
                microsoftDotNetILCompilerVersion: "7.0.0-*", // the version goes here
                nuGetFeedUrl: "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json") // this address might change over time
            .DisplayName("NativeAOT NuGet")
            .TargetFrameworkMoniker("net7.0")
            .ToToolchain()));
```

The builder allows to configure more settings:
- specify packages restore path by using `PackagesRestorePath($path)`
- rooting all application assemblies by using `RootAllApplicationAssemblies($bool)`. This is disabled by default.
- generating complete type metadata by using `IlcGenerateCompleteTypeMetadata($bool)`. This option is enabled by default.
- generating stack trace metadata by using `IlcGenerateStackTraceData($bool)`. This option is enabled by default.
- set optimization preference by using `IlcOptimizationPreference($value)`. The default is `Speed`, you can configure it to `Size` or nothing
- set instruction set for the target OS, architecture and hardware by using `IlcInstructionSet($value)`. By default BDN recognizes most of the instruction sets on your machine and enables them.

BenchmarkDotNet supports [rd.xml](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/rd-xml-format.md) files. To get given file respected by BenchmarkDotNet you need to place it in the same folder as the project that defines benchmarks and name it `rd.xml` or in case of multiple files give them `.rd.xml` extension. The alternative to `rd.xml` files is annotating types with [DynamicallyAccessedMembers](https://devblogs.microsoft.com/dotnet/app-trimming-in-net-5/) attribute.

If given benchmark is not supported by NativeAOT, you need to apply `[AotFilter]` attribute for it. Example:

```cs
[Benchmark]
[AotFilter("Not supported by design.")]
public object CreateInstanceNames() => System.Activator.CreateInstance(_assemblyName, _typeName);
```

### Generated files

By default BenchmarkDotNet removes the generates files after finishing the run. To keep them on the disk  you need to pass `--keepFiles true` command line argument or apply `[KeepBenchmarkFiles]` attribute to the class which defines benchmark(s). Then, read the folder from the tool output. In the example below it's `D:\projects\performance\artifacts\bin\MicroBenchmarks\Release\net7.0\Job-KRLVKQ`:

```log
// ***** Building 1 exe(s) in Parallel: Start   *****
// start dotnet  restore -r win-x64 /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true in D:\projects\performance\artifacts\bin\MicroBenchmarks\Release\net7.0\Job-KRLVKQ
// command took 2.74s and exited with 0
// start dotnet  build -c Release -r win-x64 --no-restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true in D:\projects\performance\artifacts\bin\MicroBenchmarks\Release\net7.0\Job-KRLVKQ
// command took 3.82s and exited with 0
```

If you go to `D:\projects\performance\artifacts\bin\MicroBenchmarks\Release\net7.0\Job-KRLVKQ`, you can see the generated project file (named `BenchmarkDotNet.Autogenerated.csproj`), code (file name ends with `.notcs`) and find the native executable (in the `bin\**\native` subfolder). Example:

```cmd
cd D:\projects\performance\artifacts\bin\MicroBenchmarks\Release\net7.0\Job-KRLVKQ
cat .\BenchmarkDotNet.Autogenerated.csproj
```

```log
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <RuntimeFrameworkVersion></RuntimeFrameworkVersion>
    <AssemblyName>Job-KRLVKQ</AssemblyName>
    <AssemblyTitle>Job-KRLVKQ</AssemblyTitle>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <PlatformTarget>x64</PlatformTarget>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugSymbols>false</DebugSymbols>
    <UseSharedCompilation>false</UseSharedCompilation>
    <Deterministic>true</Deterministic>
    <RunAnalyzers>false</RunAnalyzers>
    <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
    <TrimMode>link</TrimMode><TrimmerDefaultAction>link</TrimmerDefaultAction>
    <IlcGenerateCompleteTypeMetadata>True</IlcGenerateCompleteTypeMetadata>
    <IlcGenerateStackTraceData>True</IlcGenerateStackTraceData>
    <EnsureNETCoreAppRuntime>false</EnsureNETCoreAppRuntime>
    <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
  </PropertyGroup>
  <PropertyGroup>
    <ServerGarbageCollection>false</ServerGarbageCollection>
    <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Job-KRLVKQ.notcs" Exclude="bin\**;obj\**;**\*.xproj;packages\**" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.DotNet.ILCompiler" Version="7.0.0-*" />
    <ProjectReference Include="D:\projects\performance\src\benchmarks\micro\MicroBenchmarks.csproj" />
  </ItemGroup>
  <ItemGroup>
    <RdXmlFile Include="bdn_generated.rd.xml" />
  </ItemGroup>
  <ItemGroup>
    <IlcArg Include="--instructionset:base,sse,sse2,sse3,sse4.1,sse4.2,avx,avx2,aes,bmi,bmi2,fma,lzcnt,pclmul,popcnt" />
  </ItemGroup>
</Project>
```

### Compiling source to native code using the ILCompiler you built

If you are a NativeAOT contributor and you want to benchmark your local build of NativeAOT you have to provide necessary info (path to shipping packages).

You can do that from command line:

```cmd
dotnet run -c Release -f net7.0 --runtimes nativeaot7.0 --ilcPackages D:\projects\runtime\artifacts\packages\Release\Shipping\
```

or explicitly in the code:


```cs
var config = DefaultConfig.Instance
    .With(Job.ShortRun
        .With(NativeAotToolchain.CreateBuilder()
            .UseLocalBuild(@"C:\Projects\runtime\artifacts\packages\Release\Shipping\")
            .DisplayName("NativeAOT local build")
            .TargetFrameworkMoniker("net7.0")
            .ToToolchain()));
```

BenchmarkDotNet is going to follow [these instructrions](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/nativeaot.md#building) to get it working for you.

**Note**: BenchmarkDotNet is going to run `dotnet restore` on the auto-generated project and restore the packages to a temporary folder. It might take some time, but the next time you rebuild dotnet/runtime repo and run the same command BenchmarkDotNet is going to use the new ILCompiler package.


## Wasm

BenchmarkDotNet supports Web Assembly on Unix! However, currently you need to build the **dotnet runtime** yourself to be able to run the benchmarks.

For up-to-date docs, you should visit [dotnet/runtime repository](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing-wasm.md).

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

Install [all Mono prerequisites](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing-wasm.md):

```cmd
sudo apt-get install cmake llvm-9 clang-9 autoconf automake libtool build-essential python curl git lldb-6.0 liblldb-6.0-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libssl-dev libnuma-dev libkrb5-dev zlib1g-dev
```

And FINALLY build Mono Runtime with Web Assembly support:

```cmd
./build.sh --arch wasm --os Browser -c release
```

And that you have .NET 5 feed added to your `nuget.config` file:

```xml
<add key="dotnet5" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json" />
```

Now you should be able to run the Wasm benchmarks!

[!include[IntroWasm](../samples/IntroWasm.md)]

## MonoAotLLVM

BenchmarkDotNet supports doing Mono AOT runs with both the Mono-Mini compiler and the Mono-LLVM compiler (which uses llvm on the back end).

Using this tool chain requires the following flags:

```
--runtimes monoaotllvm
--aotcompilerpath <path to mono aot compiler>
--customruntimepack <path to runtime pack>
```

and optionally (defaults to mini)

```
--aotcompilermode <mini|llvm>  
```

As of this writing, the mono aot compiler is not available as a seperate download or nuget package. Therefore, it is required to build the compiler in the [dotnet/runtime repository].

The compiler binary (mono-sgen) is built as part of the `mono` subset, so it can be built (along with the runtime pack) like so (in the root of [dotnet/runtime]).

`./build.sh -subset mono+libs -c Release`

The compiler binary should be generated here (modify for your platform):

```
<runtime root>/artifacts/obj/mono/OSX.x64.Release/mono/mini/mono-sgen
```

And the runtime pack should be generated here:

```
<runtimeroot>artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Release/
```
