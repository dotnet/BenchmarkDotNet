# Building

There are two recommended options to build BenchmarkDotNet from source:

## Visual Studio

- [Visual Studio](https://www.visualstudio.com/downloads/) (Community, Professional, Enterprise) with .NET 4.6.1 SDK and F# support.

- [.NET 5 SDK](https://dotnet.microsoft.com/download).

Once all the necessary tools are in place, building is trivial. Simply open solution file **BenchmarkDotNet.sln** that lives at the base of the repository and run Build action.

## Cake (C# Make)

[Cake (C# Make)](https://cakebuild.net/) is a cross platform build automation system with a C# DSL to do things like compiling code, copy files/folders, running unit tests, compress files and build NuGet packages.

The build currently depends on the following prerequisites:

- Windows:
  - PowerShell version 5 or higher
  - MSBuild version 15.1 or higher
  - .NET Framework 4.6 or higher

- Linux:
  - Install [Mono version 5 or higher](https://www.mono-project.com/download/stable/#download-lin)
  - Install [fsharp package](https://fsharp.org/use/linux/)
  - Install packages required to .NET Core SDK
    - `gettext`
    - `libcurl4-openssl-dev`
    - `libicu-dev`
    - `libssl-dev`
    - `libunwind8`

- macOS
  - Install [Mono version 5 or higher](https://www.mono-project.com/download/stable/#download-mac)
  - Install [fsharp package](https://fsharp.org/use/mac/)
  - Install the latest version of [OpenSSL](https://www.openssl.org/source/).

After you have installed these pre-requisites, you can build the BenchmarkDotNet by invoking the build script (`build.ps1` on Windows, or `build.sh` on Linux and macOS) at the base of the BenchmarkDotNet repository. By default the build process also run all the tests. There are quite a few tests, taking a significant amount of time that is not necessary if you just want to experiment with changes. You can skip the tests phase by adding the `skiptests` argument to the build script, e.g. `.\build.ps1 --SkipTests=True` or `./build.sh --skiptests=true`.

Build has a number of options that you use. Some of the more important options are

- **`skiptests`** - do not run the tests. This can shorten build times quite a bit. On Windows: `.\build.ps1 --SkipTests=True` or `./build.sh --skiptests=true` on Linux/macOS.

- **`configuration`** - build the 'Release' or 'Debug' build type. Default value is 'Release'. On Windows: `.\build.ps1 -Configuration Debug` or `./build.sh --configuration debug` on Linux/macOS.

- **`target`** - with this parameter you can run a specific target from build pipeline. Default value is 'Default' target. On Windows: `.\build.ps1 -Target Default` or `./build.sh --target default` on Linux/macOS. Available targets:
  - **`Default`** - run all actions one by one.
  - **`Clean`** - clean all `obj`, `bin` and `artifacts` directories.
  - **`Restore`** - automatically execute `Clean` action and after that restore all NuGet dependencies.
  - **`Build`** - automatically execute `Restore` action, then run MSBuild for the solution file.
  - **`FastTests`** - automatically execute `Build` action, then run all tests from the BenchmarkDotNet.Tests project.
  - **`SlowTests`** - automatically execute `Build` action, then run all tests from the BenchmarkDotNet.IntegrationTests project.
  - **`Pack`** - automatically execute `Build` action and after that creates local NuGet packages.
