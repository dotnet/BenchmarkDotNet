---
uid: docs.faq
name: FAQ
---

# FAQ (Frequently asked questions)

* **Q** Why can't I install BenchmarkDotNet in Visual Studio 2010/2012/2013?

    **A** BenchmarkDotNet requires NuGet 3.x+ and can't be installed in old versions of Visual Studio which use NuGet 2.x.
Consider to use Visual Studio 2015/2017 or [Rider](https://www.jetbrains.com/rider/).
See also: [BenchmarkDotNet#237](https://github.com/dotnet/BenchmarkDotNet/issues/237), [roslyn#12780](https://github.com/dotnet/roslyn/issues/12780).

* **Q** Why can't I install BenchmarkDotNet in a new .NET Core Console App in Visual Studio 2017?

    **A** BenchmarkDotNet supports only netcoreapp2.0+.
Some old Visual Studio 2017 can create a new application which targets netcoreapp1.0.
You should upgrade it up to 2.0.
If you want to target netcoreapp1.0 in your main assembly, it's recommended to create a separated project for benchmarks.

* **Q** I created a new .NET Core Console App in Visual Studio 2017. Now I want to run my code on CoreCLR, full .NET Framework, and Mono. How can I do it?

    **A** Use the following lines in your `.csproj` file:

    ```xml
    <TargetFrameworks>netcoreapp2.0;net46</TargetFrameworks>
    <PlatformTarget>AnyCPU</PlatformTarget>
    ```

    And mark your benchmark class with the following attributes:

    ```cs
    [CoreJob, ClrJob, MonoJob]
    ```

* **Q** My source code targets old versions of .NET Framework or .NET Core, but BenchmarkDotNet requires `net461` and `netcoreapp2.0`. How can I run benchmarks in this case?

    **A** It's a good practice to introduce an additional console application (e.g. `MyAwesomeLibrary.Benchmarks`) which will depend on your code and BenchmarkDotNet.
Due to the fact that users usually run benchmarks in a develop environment and don't distribute benchmarks for users, it shouldn't be a problem.

* **Q** I wrote a small benchmark, but BenchmarkDotNet requires a lot of time for time measurements. How can I reduce this time?

    **A** By default, BenchmarkDotNet automatically chooses a number of iterations which allows achieving the best precision.
If you don't need such level of precision and just want to have a quick way to get approximated results, you can specify all parameters manually.
For example, you can use the `SimpleJob` or `ShortRunJob` attributes:

    ```cs
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5, invocationCount:100, id: "QuickJob")]
    [ShortRunJob]
    ```

* **Q** My benchmark unexpectedly stopped and I saw the information about error code. What can I do?

    **A** BenchmarkDotNet generates, builds and runs new process for every benchmark. This behavior is sometimes interpreted by anti-virus as dangerous, and the process is killed. Use `EnvironmentAnalyser` to detect antivirus software and configure your benchmark to use [`InProcessToolchain`](xref:BenchmarkDotNet.Samples.IntroInProcess).

* **Q** Can I run benchmark on the virtual machine?
 
    **A** Yes, of course. However, it can affect results because of the shared, physical machine, virtualization process and incorrect `Stopwatch.Frequency`. If you are unsure whether an application is running on virtual environment, use `EnvironmentAnalyser` to detect VM hypervisor.

* **Q** I have failed to run my benchmarks, I am getting following errors about non-optimized dll. What can I do?  

    ```
    Assembly BenchmarkDotNet.Samples which defines benchmarks references non-optimized BenchmarkDotNet
            If you own this dependency, please, build it in RELEASE.
            If you don't, you can create custom config with DontFailOnError to disable our custom policy and allow this b
    Assembly BenchmarkDotNet.Samples which defines benchmarks is non-optimized
    Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE.
    ```

    **A** You should always run your benchmarks in RELEASE mode with optimizations enabled (default setting for RELEASE). However if you have to use non-optimized 3rd party assembly you have to create custom config to disable our default policy.

    ```cs
    public class AllowNonOptimized : ManualConfig
    {
        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }
    ```

* **Q** I have failed to run my benchmarks from LINQPad. How can I fix this problem?  

    ```
    Assembly LINQPadQuery which defines benchmarks references non-optimized LINQPad
    Assembly LINQPadQuery which defines benchmarks is non-optimized
    Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE.
    ```

    **A** You need to make sure that you are using **AnyCPU** 5.22.05+ build of LINQPad with optimizations enabled. To enable the optimizations you need to go to Preferences -> Query and select `compile with /optimize+`

* **Q** I'm trying to use `RPlotExporter` but there are no any images in the `results` folder

    **A** Try to specify `R_LIBS_USER` (e.g. `R_LIBS_USER=/usr/local/lib/R/` on Linux/macOS, see also: [#692](https://github.com/dotnet/BenchmarkDotNet/issues/692))

* **Q** My benchmark failed with OutOfMemoryException. How can I fix this problem? 

    **A** BenchmarkDotNet continues to run additional iterations until desired accuracy level is achieved. It's possible only if the benchmark method doesn't have any side-effects. 
    If your benchmark allocates memory and keeps it alive, you are creating a memory leak. 
    
    You should redesign your benchmark and remove the side-effects. You can use `OperationsPerInvoke`, `IterationSetup` and `IterationCleanup` to do that.
    
    An example:
    
    ```cs
    public class OOM
    {
        private StringBuilder buffer = new StringBuilder();
        
        [Benchmark]
        public void HasSideEffects()
        {
            // This method is growing the buffer to infinity
            // because it's executed millions of times
            buffer.Append('a');
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public void HasNoSideEffects()
        {
            buffer.Clear();
    
            for (int i = 0; i < 1024; i++)
                buffer.Append('a');
        }
    }
    ```    
