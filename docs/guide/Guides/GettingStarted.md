# Getting started

To get started with BenchmarkDotNet, please follow these steps. 

## Step 1. Installation
Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```
PM> Install-Package BenchmarkDotNet
```


## Step 2. Design a benchmark
Create a new console application, write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we 
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

## Step 3. View results
View the results. Here is an example of output from the above benchmark:

```ini
BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4702MQ CPU 2.20GHz, ProcessorCount=8
Frequency=2143476 Hz, Resolution=466.5319 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  DefaultJob : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
```

| Method | Mean        | StdDev    | Allocated |
| ------ | ----------- | --------- | --------- |
| Sha256 | 130.5169 us | 1.8489 us | 188 B     |
| Md5    | 25.8010 us  | 0.1757 us | 113 B     |


## Step 4. Analyze results

Analyze it. In your bin directory, you can find a lot of useful files with detailed information. For example:

* Csv reports with raw data: `Md5VsSha256-report.csv`, `Md5VsSha256-runs.csv`
* Markdown reports:  `Md5VsSha256-report-default.md`, `Md5VsSha256-report-stackoverflow.md`, `Md5VsSha256-report-github.md`
    * Plain report and log: `Md5VsSha256-report.txt`, `Md5VsSha256.log`
    * Plots (if you have installed R): `Md5VsSha256-barplot.png`, `Md5VsSha256-boxplot.png`, and so on.

## Next steps

BenchmarkDotNet provides a lot of features which help to high-quality performance research.
If you want to know more about BenchmarkDotNet features, checkout the [Overview](../Overview.htm) page.
If you want have any questions, checkout the [FAQ](../FAQ.htm) page.
If you didn't find answer for your question on this page, [ask it on gitter](https://gitter.im/dotnet/BenchmarkDotNet) or [create an issue](https://github.com/dotnet/BenchmarkDotNet/issues).