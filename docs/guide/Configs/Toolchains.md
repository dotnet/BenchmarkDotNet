# Toolchains

In BenchmarkDotNet we generate, build and execute new console app per every benchmark. A **toolchain** contains generator, builder, and executor. 

When you run your benchmarks without specifying the toolchain in an explicit way, we use the default one. It works OOTB, you don't need to worry about anything.

We use Roslyn for classic .NET and Mono, and `dotnet cli` for .NET Core.

## Multiple frameworks support

You can target multiple frameworks with single, modern csproj file:

```xml
<TargetFrameworks>netcoreapp2.0;net462</TargetFrameworks>
```

BenchmarkDotNet allows you to take full advantage of that. With single config, we can execute the benchmarks for all the frameworks that you have listed in your csproj file.

If you specify `Runtime` in explicit way, we just choose the right toolchain for you.

```cs
[ClrJob, MonoJob, CoreJob]
public class Algo_Md5VsSha256
{
    // the benchmarks are going to be executed for classic .NET, Mono (default path) and .NET Core
}
```

### TFM

At some point of time we need to choose the target framework moniker (TFM).

When you are running your app with benchmark as .NET Core app, we just check the version of the `System.Runtime.dll` which allows us to decide which version of .NET Core you are using.

But when you are running your project as classic .NET (.NET 4.6.2 for example), we don't know which TFM to choose for your .NET Core Runtime, so we use the default one - **netcoreapp1.1**.

If the default `netcoreapp1.1` is not OK for you, you must configure the toolchains in explicit way:

```cs
public class MultipleRuntimes : ManualConfig
{
    public MultipleRuntimes()
    {
        Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp20)); // .NET Core 2.0

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

## InProcessToolchain

InProcessToolchain is our toolchain which does not generate any new executable. It emits IL on the fly and runs it from within the process itself. It can be usefull if want to run the benchmarks very fast or if you want to run them for framework which we don't support. An example could be a local build of CoreCLR.

```cs
[Config(typeof(Config))]
public class IntroInProcess
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(Job.MediumRun
                .WithLaunchCount(1)
                .WithId("OutOfProc"));

            Add(Job.MediumRun
                .WithLaunchCount(1)
                .With(InProcessToolchain.Instance)
                .WithId("InProcess"));
        }
    }

    [Benchmark(Description = "new byte[10kB]")]
    public byte[] Allocate() => new byte[10000];
}
```

or just:

```cs
[InProcessAttribute]
public class TypeWithBenchmarks
{
}
```