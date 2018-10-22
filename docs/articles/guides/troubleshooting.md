# Troubleshooting

## BenchmarkDotNet

You need to be aware of the fact that to ensure process-level isolation BenchmarkDotNet generates, builds and executes every benchmark in a dedicated process. For .NET and Mono we generate a C# file and compile it using Roslyn. For .NET Core and CoreRT we generate not only C# file but also a project file which later is restored and build with dotnet cli. If your project has some non-trivial build settings like a `.props` and `.target` files or native dependencies things might not work well out of the box.

How do you know that BenchmarkDotNet has failed to build the project? BDN is going to tell you about it. An example:

```log
  // ***** BenchmarkRunner: Start   *****
  // ***** Building 1 exe(s) in Parallel: Start   *****
  // msbuild /p:ConfigurationGroup=Release  /p:UseSharedCompilation=false took 12,93s and exited with 1
  // ***** Done, took 00:00:15 (15.59 sec)   *****
  // Found benchmarks:
  //   Perf_Console.OpenStandardInput: Job-ZAFVFJ(Force=True, Toolchain=CoreFX, IterationCount=3, LaunchCount=1, WarmupCount=3)

  // Build Exception: Microsoft (R) Build Engine version 15.9.8-preview+g0a5001fc4d for .NET Core
  Copyright (C) Microsoft Corporation. All rights reserved.

  C:\Program Files\dotnet\sdk\2.2.100-preview2-009404\Microsoft.Common.CurrentVersion.targets(4176,5): warning MSB3026: Could not copy "C:\Projects\corefx/bin/obj/AnyOS.AnyCPU.Release/BenchmarksRunner/netstandard/BenchmarksRunner.exe" to "C:\Projects\corefx\bin/AnyOS.AnyCPU.Release/BenchmarksRunner/netstandard/BenchmarksRunner.exe". Beginning retry 1 in 1000ms. The process cannot access the file 'C:\Projects\corefx\bin\AnyOS.AnyCPU.Release\BenchmarksRunner\netstandard\BenchmarksRunner.exe' because it is being used by another process.  [C:\Projects\corefx\src\Common\perf\BenchmarksRunner\BenchmarksRunner.csproj]
```

If the error message is not clear enough, you need to investigate it further.

How to troubleshoot the build process:

1. Configure BenchmarkDotNet to keep auto-generated benchmark files (they are being removed after benchmark is executed by default). You can do that by either passing `--keepFiles` console argument to `BenchmarkSwitcher` or by using `[KeepBenchmarkFiles]` attribute on the type which defines the benchmarks or by using `config.KeepBenchmarkFiles()` extension method.
2. Run the benchmarks
3. Go to the output folder, which typicaly is `bin\Release\$FrameworkMoniker` and search for the new folder with auto-generated files. The name of the folder is just Job's ID. So if you are using `--job short` the folder should be called "ShortRun". If you want to change the name, use `Job.WithId("$newID")` extension method.
4. The folder should contain: 
   * a file with source code (ends with `.notcs` to make sure IDE don't include it in other projects by default)
   * a project file (`.csproj`)
   * a script file (`.bat` on Windows, `.sh` for other OSes) which should be doing exactly the same thing as BDN does:
     * dotnet restore
     * dotnet build (with some parameters like `-c Release`)
5. Run the script, read the error message. From here you continue with the troubleshooting like it was a project in your solution.

The recommended order of solving build issues:

1. Change the right settings in your project file which defines benchmarks to get it working.
2. Customize the `Job` settings using available options like `job.WithCustomBuildConfiguration($name)`or `job.With(new Argument[] { new MsBuildArgument("/p:SomeProperty=Value")})`.
3. Implement your own `IToolchain` and generate and build all the right things in your way (you can use existing Builders and Generators and just override some methods to change specific behaviour).
4. Report a bug in BenchmarkDotNet repository.

## Debugging Benchmarks

## In the same process

If your benchmark builds but fails to run, you can simply debug it. The first thing you should try is to do it in a single process (host process === runner process).

1. Use `DebugInProcessConfig`

```cs
static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
```

2. Set the breakpoints in your favorite IDE
3. Start debugging the project with benchmarks

## In a different process

Sometimes you won't be able to reproduce the problem in the same process. In this case, you have 3 options:

### Launch a debugger from the benchmark process using Debugger API

```cs
[GlobalSetup]
public void Setup()
{
    System.Diagnostics.Debugger.Launch();
}
```

### Attach a debugger from IDE

Modify your benchmark to sleep until the Debugger is not attached and use your favorite IDE to attach the debugger to benchmark process. **Do attach to the process which is running the benchmark** (the arguments of the process are going to be `--benchmarkId $someNumber --benchmarkName $theName`), not the host process.

```cs
[GlobalSetup]
public void Setup()
{
    while(!System.Diagnostics.Debugger.IsAttached)
        Thread.Sleep(TimeSpan.FromMilliseconds(100));
}
```

### One of the above, but with a Debug build

By default, BDN builds everything in Release. But debugging Release builds even with full symbols might be non-trivial. To enforce BDN to build the benchmark in Debug please use `DebugBuildConfig` and then attach the debugger.

