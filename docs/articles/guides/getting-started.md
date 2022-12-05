# Getting started
To get started with BenchmarkDotNet, please follow these steps. 

## Step 1. Create a project
Create a new console application.

## Step 2. Installation
Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```cmd
> dotnet add package BenchmarkDotNet
```

Read more about BenchmarkDotNet NuGet packages: @docs.nuget

## Step 3. Design a benchmark
Write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we 
compare [MD5](https://en.wikipedia.org/wiki/MD5) and [SHA256](https://en.wikipedia.org/wiki/SHA-2) cryptographic hash functions:

```cs
using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace MyBenchmarks
{
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Md5VsSha256>();
        }
    }
}
```

The `BenchmarkRunner.Run<Md5VsSha256>()` call runs your benchmarks and prints results to the console.

## Step 4. Run benchmarks
Start your console application to run the benchmarks. The application must be built in the Release configuration.

```cmd
> dotnet run -c Release
```

## Step 5. View results
View the results. Here is an example of output from the above benchmark:

```
BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.2251)
Intel Core i7-4770HQ CPU 2.20GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


| Method |     Mean |    Error |   StdDev |
|------- |---------:|---------:|---------:|
| Sha256 | 51.57 us | 0.311 us | 0.291 us |
|    Md5 | 21.91 us | 0.138 us | 0.129 us |
```

## Step 6. Analyze results
BenchmarkDotNet will automatically create basic reports in the `.\BenchmarkDotNet.Artifacts\results` folder that can be shared and analyzed. 

To help analyze performance in further depth, you can configure your benchmark to collect and output more detailed information. Benchmark configuration can be conveniently changed by adding attributes to the class containing your benchmarks. For example:

* [Diagnosers](../configs/diagnosers.md)
  * GC allocations: `[MemoryDiagnoser]`
  * Code size and disassembly: `[DisassemblyDiagnoser]`
  * Threading statistics: `[ThreadingDiagnoser]`
* [Exporters](../configs/exporters.md)
  * CSV reports with raw data: `[CsvMeasurementsExporter]`
  * JSON reports with raw data: `[JsonExporter]`
  * Plots (if you have installed R): `[RPlotExporter]`

For more information, see [Configs](../configs/configs.md).

## Next steps
BenchmarkDotNet provides features which aid high-quality performance research.
If you want to know more about BenchmarkDotNet features, check out the [Overview](../overview.md) page.
If you have any questions, check out the [FAQ](../faq.md) page.
If you didn't find an answer for your question on this page, [ask it on Gitter](https://gitter.im/dotnet/BenchmarkDotNet) or [create an issue on GitHub](https://github.com/dotnet/BenchmarkDotNet/issues).
