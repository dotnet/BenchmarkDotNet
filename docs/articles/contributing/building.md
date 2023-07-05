# Building

There are two recommended options to build BenchmarkDotNet from source:

## Visual Studio

- [Visual Studio](https://www.visualstudio.com/downloads/) (Community, Professional, Enterprise) with .NET 4.6.2 SDK and F# support.

- [.NET 7 SDK](https://dotnet.microsoft.com/download).

Once all the necessary tools are in place, building is trivial. Simply open solution file **BenchmarkDotNet.sln** that lives at the base of the repository and run Build action.

## Command-line

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

In order to run various build tasks from terminal, use `build.cmd` file in the repository root.
`build.cmd` is a cross-platform script that can be used the same way on Windows, Linux, and macOS.
When executed without arguments, it prints help information with list of all available build tasks.