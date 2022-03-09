# Getting started

To get started with BenchmarkDotNet, please follow these steps. 

## Step 1. Create a project
Create a new console application.

## Step 2. Installation

Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```
PM> Install-Package BenchmarkDotNet
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

The `BenchmarkRunner.Run<Md5VsSha256>()` call runs your benchmarks and print results to console output.

## Step 4. View results
View the results. Here is an example of output from the above benchmark:

```
BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.472 (1803/April2018Update/Redstone4)
Intel Core i7-2630QM CPU 2.00GHz (Sandy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=1948699 Hz, Resolution=513.1629 ns, Timer=TSC
.NET Core SDK=2.1.502
  [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT


| Method |      Mean |     Error |    StdDev |
|------- |----------:|----------:|----------:|
| Sha256 | 100.90 us | 0.5070 us | 0.4494 us |
|    Md5 |  37.66 us | 0.1290 us | 0.1207 us |
```


## Step 5. Analyze results

Analyze it. In your bin directory, you can find a lot of useful files with detailed information. For example:

* Csv reports with raw data: `Md5VsSha256-report.csv`, `Md5VsSha256-runs.csv`
* Markdown reports:  `Md5VsSha256-report-default.md`, `Md5VsSha256-report-stackoverflow.md`, `Md5VsSha256-report-github.md`
    * Plain report and log: `Md5VsSha256-report.txt`, `Md5VsSha256.log`
    * Plots (if you have installed R): `Md5VsSha256-barplot.png`, `Md5VsSha256-boxplot.png`, and so on.

## Next steps

BenchmarkDotNet provides a lot of features which help to high-quality performance research.
If you want to know more about BenchmarkDotNet features, checkout the [Overview](../overview.md) page.
If you want have any questions, checkout the [FAQ](../faq.md) page.
If you didn't find answer for your question on this page, [ask it on gitter](https://gitter.im/dotnet/BenchmarkDotNet) or [create an issue](https://github.com/dotnet/BenchmarkDotNet/issues).
