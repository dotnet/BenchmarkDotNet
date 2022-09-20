---
uid: docs.nuget
name: Installing NuGet packages
---

# Installing NuGet packages

## Packages

We have the following set of NuGet packages (you can install it directly from `nuget.org`):

* `BenchmarkDotNet`: Basic BenchmarkDotNet infrastructure and logic. This is all you need to run benchmarks.
* `BenchmarkDotNet.Diagnostics.Windows`: an additional optional package that provides a set of Windows diagnosers.
* `BenchmarkDotNet.Templates`: Templates for BenchmarkDotNet.

You might find other NuGet packages that start with `BenchmarkDotNet` name, but they are internal BDN packages that should not be installed manually. All that matters are the three packages mentioned above.

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
