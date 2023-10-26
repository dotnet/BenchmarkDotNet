---
uid: docs.dotnet-new-templates
name: BenchmarkDotNet templates 
---

# BenchmarkDotNet templates

BenchmarkDotNet provides project templates to setup your benchmarks easily.  
The template exists for each major .NET language ([C#](https://learn.microsoft.com/dotnet/csharp/), [F#](https://learn.microsoft.com/dotnet/fsharp/) and [VB](https://learn.microsoft.com/dotnet/visual-basic/)) with equivalent features and structure.

## How to install the templates

The templates requires the [.NET Core SDK](https://www.microsoft.com/net/download). Once installed, run the following command to install the templates:

```log
dotnet new install BenchmarkDotNet.Templates
```

If you want to uninstall all BenchmarkDotNet templates:

```log
dotnet new uninstall BenchmarkDotNet.Templates
```

The template is a nuget package distributed over nuget: [BenchmarkDotNet.Templates](https://www.nuget.org/packages/BenchmarkDotNet.Templates/).

## Basic usage

To create a new C# benchmark library project from the template, run:

```log
dotnet new benchmark
```

 If you'd like to create F# or VB project, you can specify project language with `-lang` option:

```log
dotnet new benchmark -lang F#
dotnet new benchmark -lang VB
```

## Project template specific options

The template projects has five additional options - all of them are optional.

By default a console app project targeting `net6.0` is created.
This lets you run the benchmarks from console (`dotnet run`) or from your favorite IDE.  

The option `-f` or `--framework` changes the target framework:

```log
dotnet new benchmark -f net472
```

You can specify `--console-app=false` to create a class library project targeting `netstandard2.0` by default:

```log
dotnet new benchmark --console-app=false
```

The option `-b` or `--benchmarkName` sets the name of the benchmark class:

```log
dotnet new benchmark -b Md5VsSha256
```

BenchmarkDotNet lets you create a dedicated configuration class (see [Configs](xref:docs.configs)) to customize the execution of your benchmarks.
To create a benchmark project with a configuration class, use the option `-c` or `--config`:

```log
dotnet new benchmark -c
```

The option `--no-restore` if specified, skips the automatic nuget restore after the project is created:

```log
dotnet new benchmark --no-restore
```

Use the `-h` or `--help` option to display all possible arguments with a description and the default values:

```log
dotnet new benchmark --help
```

## How to run the benchmarks

Please read [how to run your benchmarks](xref:docs.how-to-run).

## The relationship of BenchmarkDotNet and BenchmarkDotNet.Templates

The version of the template nuget package is synced with the [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/) package.
For instance, the template version `0.11.5` is referencing [BenchmarkDotnet 0.11.15](https://www.nuget.org/packages/BenchmarkDotNet/0.11.5) - there is no floating version behavior.

**Note**: This will maybe change when BenchmarkDotNet reaches `1.x`.

## References

For more info about the `dotnet new` CLI, please read [the documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet).

