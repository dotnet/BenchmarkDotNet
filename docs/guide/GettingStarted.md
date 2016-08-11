# Getting started

To get started with BenchmarkDotNet, please follow these steps. 

## Step 1. Installation
Install BenchmarkDotNet via the NuGet package: [BenchmarkDotNet](https://www.nuget.org/packages/BenchmarkDotNet/)

```
PM> Install-Package BenchmarkDotNet
```


## Step 2. Write code to benchmark
Write a class with methods that you want to measure and mark them with the `Benchmark` attribute. In the following example, we 
compare [MD5](https://en.wikipedia.org/wiki/MD5) and [SHA256](https://en.wikipedia.org/wiki/SHA-2) cryptographic hash functions:

```cs
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
    public byte[] Sha256()
    {
        return sha256.ComputeHash(data);
    }

    [Benchmark]
    public byte[] Md5()
    {
        return md5.ComputeHash(data);
    }
}
```

## Step 3. Run the benchmark

Run it:

```cs
var summary = BenchmarkRunner.Run<Md5VsSha256>();
```

## Step 4. View results
View the results. Here is an example of output from the above benchmark:

```ini
BenchmarkDotNet=v0.9.0.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-4810MQ CPU @ 2.80GHz, ProcessorCount=8
Frequency=2728067 ticks, Resolution=366.5599 ns
HostCLR=MS.NET 4.0.30319.42000, Arch=64-bit RELEASE [RyuJIT]

Type=Md5VsSha256  Mode=Throughput
```

 Method |      Median |    StdDev
------- |------------ |----------
    Md5 |  21.2912 us | 0.4373 us
 Sha256 | 107.4124 us | 1.8339 us


## Step 5. Analyze results

Analyze it. In your bin directory, you can find a lot of useful files with detailed information. For example:

  * Csv reports with raw data: `Md5VsSha256-report.csv`, `Md5VsSha256-runs.csv`
  * Markdown reports:  `Md5VsSha256-report-default.md`, `Md5VsSha256-report-stackoverflow.md`, `Md5VsSha256-report-github.md`
  * Plain report and log: `Md5VsSha256-report.txt`, `Md5VsSha256.log`
  * Plots (if you have installed R): `Md5VsSha256-barplot.png`, `Md5VsSha256-boxplot.png`, and so on.
