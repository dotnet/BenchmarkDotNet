---
uid: docs.vstest
name: Running with VSTest
---

# Running with VSTest

BenchmarkDotNet supports discovering and executing benchmarks through VSTest.
This provides an alternative user experience to running benchmarks with the CLI
  and may be preferable for those who like their IDE's VSTest integrations that they may have used when running unit tests.

Below is an example of running some benchmarks from the BenchmarkDotNet samples project in Visual Studio's Test Explorer.

![](../../images/vs-testexplorer-demo.png)

## About VSTest

VSTest is one of the most popular test platforms in use in the .NET ecosystem,
  with test frameworks such as MSTest, xUnit, and NUnit providing support for it.
Many IDEs, including Visual Studio and Rider, provide UIs for running tests through VSTest
  which some users may find more accessible than running them through the command line.

It may seem counterintuitive to run performance tests on a platform
  that is designed for unit tests that expect a boolean outcome of "Passed" or "Failed".
However, VSTest provides good value as a protocol for discovering and executing tests.
In addition, we can still make use of this boolean output to indicate
  if the benchmark had validation errors that caused it to fail to run.

## Caveats and things to know

* **The benchmark measurements may be affected by the VSTest host and your IDE!**  
  If you want to have accurate measurements,
    it is recommended to run benchmarks through the CLI without other processes on the machine impacting performance.
  This does not mean that the measurements are useless though,
    it will still be able to provide useful measurements during development when comparing different approaches.
* **The test adapter will not display or execute benchmarks if optimizations are disabled.**  
  Please ensure you are compiling in Release mode or with `Optimize` set to true.
  Using an `InProcess` toolchain will let you run your benchmarks with optimizations disabled
    and will let you attach the debugger as well.
* **The VSTest adapter will not call your application's entry point.**  
  If you use the entry point to customize how your benchmarks are run,
    you will need to do this through other means such as an assembly-level `IConfigSource`,
    as shown [here](#setting-a-default-configuration).
* **The test adapter will generate an entry point for you automatically.**  
  The generated entry point will pass the command line arguments
    and the current assembly into `BenchmarkSwitcher`,
    so you can still use it in your CLI as well as in VSTest.
  This means you can delete your entry point and only need to define your benchmarks.
  If you want to use a custom entry point, you can still do so by setting `GenerateProgramFile` to `false` in your project file.

## Getting started

* **Step 1.** Install the NuGet packages.  
  You need to install two packages into your benchmark project:
  * `BenchmarkDotNet.TestAdapter`: Implements the VSTest protocol for BenchmarkDotNet
  * `Microsoft.NET.Test.Sdk`: Includes all the pieces needed for the VSTest host to run and load the VSTest adapter.
* **Step 2.** Make sure that the entry point is configured correctly.  
  As mentioned in the caveats section, `BenchmarkDotNet.TestAdapter` will generate an entry point for you automatically.
  So, if you have an entry point already,
    you will either need to delete it or set `GenerateProgramFile` to `false` in your project file to continue using your existing one.
  Here is an example of a `.csproj` file based on the default Console Application template:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Disable entry point generation as this project has it's own entry point -->
    <GenerateProgramFile>false</GenerateProgramFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet.TestAdapter" Version="0.13.12" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

</Project>
```

* **Step 3.** Make sure that your IDE supports VSTest integration.  
  In Visual Studio, everything works out of the box.
  In Rider/R# 2023.3, the VSTest integration should be activated:
  * Go to the "Unit Testing" settings page.
    * Rider: Settings -> Build, Execution, Deployment -> Unit Testing -> VSTest
    * R#: Extensions -> ReSharper -> Options -> Tools -> Unit Testing -> Test Frameworks -> VSTest
  * Make sure that the "Enable VSTest adapter support" checkbox is checked.
  In future versions of Rider, this should be enabled by default.
* **Step 4.** Switch to the `Release` configuration.  
  As mentioned above, the TestAdapter is not able to discover and run benchmarks with optimizations disabled (by design).
* **Step 5.** Build the project.  
  In order to discover the benchmarks, the VSTest adapter needs to be able to find the assembly.
  Once you build the project, you should observe the discovered benchmarks in your IDE's Unit Test Explorer.

If you correctly performed all the steps above, you should be able to run your benchmarks in your IDE using embedded unit testing features.
If this doesn't work for you, don't hesitate to file [a new GitHub issue](https://github.com/dotnet/BenchmarkDotNet/issues/new).

## Setting a default configuration

Previously, it was common for the default configuration to be defined inside the entry point.
Since the entry point is not used when running benchmarks through VSTest,
  the default configuration must be specified using a `Config` attribute that is set on the assembly instead.

First, create a class that extends `ManualConfig` or `IConfig` which sets the default configuration you want:

```csharp
class MyDefaultConfig : ManualConfig
{
    public MyDefaultConfig()
    {
        AddJob(Job.Dry);
        AddLogger(Loggers.ConsoleLogger.Default);
        AddValidator(JitOptimizationsValidator.DontFailOnError);
    }
}
```

Then, set an assembly attribute with the following.

```csharp
[assembly: Config(typeof(MyDefaultConfig))]
```

By convention, assembly attributes are usually defined inside `AssemblyInfo.cs` in a directory called `Properties`.

## Viewing the results

The full output from BenchmarkDotNet that you would have been used to seeing in the past will be sent to the "Tests" output of your IDE.
Use this view if you want to see the tabular view that compares multiple benchmarks with each other or
  if you want to see the results for each individual iteration.

One more place where you can view the results is in each individual test's output messages.
In Visual Studio, this can be viewed by clicking on the test in the Test Explorer after running it and looking at the Test Detail Summary.
Since this only displays statistics for a single benchmark case,
  it does not show the tabulated view that compares multiple benchmark cases.
Instead, it displays a histogram and various other useful statistics.
Not all IDEs support displaying these output messages, so you may only be able to view the results using the "Tests" output.
