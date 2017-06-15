# Installing NuGet packages

## Packages

We have the following set of NuGet packages (you can install it directly from `nuget.org`):

* `BenchmarkDotNet.Core`: basic BenchmarkDotNet infrastructure and logic. Doesn't have any dependencies.
* `BenchmarkDotNet.Toolchains.Roslyn`: a package that includes `RoslynToolchain` which adds an ability to build your benchmarks with the Roslyn compiler. Depends on a set of additional NuGet packages.
* `BenchmarkDotNet`: an ultimate package that depends on `BenchmarkDotNet.Core` and `BenchmarkDotNet.Toolchains.Roslyn`: provides the `BenchmarkRunner`. In 99% of situations, you should start with this package.
* `BenchmarkDotNet.Diagnostics.Windows`: an additional optional package that provides a set of Windows diagnosers.

## Versioning system and feeds
We have 3 kinds of versions: *stable*, *nightly*, and *develop*.
You can get the current version from the source code via `BenchmarkDotNetInfo.FullVersion` and the full title via `BenchmarkDotNetInfo.FullTitle`.

### Stable
These versions are available from the official NuGet feed.

```xml
<packageSources>
  <add key="api.nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
</packageSources>
```

* Example of the main NuGet package: `BenchmarkDotNet.0.10.3.nupkg`.
* Example of `BenchmarkDotNetInfo.FullTitle`: `BenchmarkDotNet v0.10.3`.

### Nightly
If you want to use a nightly version of the BenchmarkDotNet, add the `https://ci.appveyor.com/nuget/benchmarkdotnet` feed in the `<packageSources>` section of your `NuGet.config`:

```xml
<packageSources>
  <add key="bdn-nightly" value="https://ci.appveyor.com/nuget/benchmarkdotnet" />
</packageSources>
```

Now you can install the packages from the `bdn-nightly` feed.

* Example of the main NuGet package: `BenchmarkDotNet.0.10.3.13.nupkg`.
* Example of `BenchmarkDotNetInfo.FullTitle`: `BenchmarkDotNet v0.10.3.13-nightly`.

### Develop
You also can build BenchmarkDotNet from source code.
The `.nupkg` files could be build with the help of `.\build\build-and-pack.cmd`.

* Example of the main NuGet package: `BenchmarkDotNet.0.10.3-develop.nupkg`.
* Example of `BenchmarkDotNetInfo.FullTitle`: `BenchmarkDotNet v0.10.3.20170304-develop`.