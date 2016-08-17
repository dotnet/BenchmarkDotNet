# NuGet Packages

We have the following set of NuGet packages (you can install it directly from `nuget.org`):

* `BenchmarkDotNet.Core`: basic BenchmarkDotNet infrastructure and logic. Doesn't have any dependencies.
* `BenchmarkDotNet.Toolchains.Roslyn`: a package that includes `RoslynToolchain` which adds an ability to build your benchmarks with the Roslyn compiler. Denends on a set of additional NuGet packages.
* `BenchmarkDotNet`: an ultimate package that depends on `BenchmarkDotNet.Core` and `BenchmarkDotNet.Toolchains.Roslyn`: provides the `BenchmarkRunner`. In 99% of situations, you should start with this package.
* `BenchmarkDotNet.Diagnostics.Windows`: an additional optional package that provides a set of Windows diagnosers.

## Private feed

If you want to check the develop version of the BenchmarkDotNet NuGet packages, add the following line in the `<packageSources>` section of your `NuGet.congig`:
```xml
<add key="appveyor-bdn" value="https://ci.appveyor.com/nuget/benchmarkdotnet" />
```
Now you can install the packages from the `appveyor-bdn` feed.