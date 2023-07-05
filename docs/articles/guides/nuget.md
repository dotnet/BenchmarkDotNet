---
uid: docs.nuget
name: Installing NuGet packages
---

# Installing NuGet packages

## Packages

We have the following set of NuGet packages (you can install it directly from `nuget.org`):

* `BenchmarkDotNet`: BenchmarkDotNet infrastructure and logic. This is all you need to run benchmarks.
* `BenchmarkDotNet.Annotations`: Basic BenchmarkDotNet annotations for your benchmarks.
* `BenchmarkDotNet.Diagnostics.Windows`: an additional optional package that provides a set of Windows diagnosers.
* `BenchmarkDotNet.Diagnostics.dotTrace`: an additional optional package that provides DotTraceDiagnoser.
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

### Nightly

If you want to use a nightly version of the BenchmarkDotNet, add the `https://www.myget.org/F/benchmarkdotnet/api/v3/index.json` feed in the `<packageSources>` section of your `NuGet.config`:

```xml
<packageSources>
  <add key="bdn-nightly" value="https://www.myget.org/F/benchmarkdotnet/api/v3/index.json" />
</packageSources>
```

Now you can install the packages from the `bdn-nightly` feed.

### Develop

You also can build BenchmarkDotNet from source code:

```sh
build.cmd pack
```