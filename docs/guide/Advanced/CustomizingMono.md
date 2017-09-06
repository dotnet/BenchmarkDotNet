# Customizing Mono

## Custom Mono Path(s)

BenchmarkDotNet allows you to compare different runtimes, including Mono. If you apply `[MonoJob]` attribute to your class we use your default mono runtime. If you want to compare different versions of Mono you need to provide use the custom paths. You can do this today by using the overloaded ctor of MonoJob attribute or by specifying the runtime in a fluent way.

### Sample configuration

```cs
[ClrJob, CoreJob]
[MonoJob("Mono 4.8.0", @"C:\Program Files\Mono\bin\mono.exe")]
[MonoJob("Mono 4.6.2", @"C:\Program Files (x86)\Mono\bin\mono.exe")]
public class Algo_Md5VsSha256
{
   /* benchmarks removed for brevity */
}	
```

```cs
private IConfig CreateConfig()
{
	return ManualConfig.CreateEmpty()
		.With(Job.ShortRun.With(new MonoRuntime("Mono 4.8.0", @"C:\Program Files\Mono\bin\mono.exe"))
		.With(Job.ShortRun.With(new MonoRuntime("Mono 4.6.2", @"C:\Program Files (x86)\Mono\bin\mono.exe"));
}
```

### Sample results:

```
// ***** BenchmarkRunner: Start   *****
// Found benchmarks:
//   Algo_Md5VsSha256.Md5: Clr(Runtime=Clr)
//   Algo_Md5VsSha256.Sha256: Clr(Runtime=Clr)
//   Algo_Md5VsSha256.Md5: Core(Runtime=Core)
//   Algo_Md5VsSha256.Sha256: Core(Runtime=Core)
//   Algo_Md5VsSha256.Md5: Mono 4.6.2(Runtime=Mono 4.6.2)
//   Algo_Md5VsSha256.Sha256: Mono 4.6.2(Runtime=Mono 4.6.2)
//   Algo_Md5VsSha256.Md5: Mono 4.8.0(Runtime=Mono 4.8.0)
//   Algo_Md5VsSha256.Sha256: Mono 4.8.0(Runtime=Mono 4.8.0)
```

``` ini
BenchmarkDotNet=v0.10.2.0-develop, OS=Microsoft Windows 10.0.14393
Processor=Intel(R) Core(TM) i7-6600U CPU 2.60GHz, ProcessorCount=4
Frequency=2742190 Hz, Resolution=364.6720 ns, Timer=TSC
dotnet cli version=1.0.0-rc4-004771
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Clr        : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Core       : .NET Core 4.6.24628.01, 64bit RyuJIT
  Mono 4.6.2 : Mono 4.6.2 (Visual Studio built mono), 32bit 
  Mono 4.8.0 : Mono 4.8.0 (Visual Studio built mono), 64bit 
```

| Method |        Job |    Runtime |        Mean |     StdDev |      Median | Scaled | Scaled-StdDev | Allocated |
|------- |----------- |----------- |------------ |----------- |------------ |------- |-------------- |---------- |
|    Md5 |        Clr |        Clr |  23.2459 us |  0.9337 us |  23.3425 us |   1.00 |          0.00 |     112 B |
| Sha256 |        Clr |        Clr | 111.8421 us |  2.4760 us | 110.6653 us |   4.82 |          0.21 |     188 B |
|    Md5 |       Core |       Core |  21.4333 us |  0.5074 us |  21.1727 us |   1.00 |          0.00 |      80 B |
| Sha256 |       Core |       Core |  50.1553 us |  1.2729 us |  49.7332 us |   2.34 |          0.08 |     114 B |
|    Md5 | Mono 4.6.2 | Mono 4.6.2 |  45.1584 us |  1.0023 us |  45.5757 us |   1.00 |          0.00 |       N/A |
| Sha256 | Mono 4.6.2 | Mono 4.6.2 | 168.4400 us |  3.7425 us | 170.4372 us |   3.73 |          0.11 |       N/A |
|    Md5 | Mono 4.8.0 | Mono 4.8.0 |  40.4432 us |  0.9094 us |  40.2218 us |   1.00 |          0.00 |       N/A |
| Sha256 | Mono 4.8.0 | Mono 4.8.0 | 155.5073 us | 12.9351 us | 151.4520 us |   3.85 |          0.33 |       N/A |

## Mono Options

Mono supports many custom options: `mono [options] program [program-options]` And so does the BenchmarkDotNet. You can configure the options by using `MonoArgument`.

### Sample configuration

```cs
[Config(typeof(ConfigWithCustomArguments))]
public class IntroCustomMonoArguments
{
    public class ConfigWithCustomArguments : ManualConfig
    {
        public ConfigWithCustomArguments()
        {
            // --optimize=MODE , -O=mode
            // MODE is a comma separated list of optimizations. They also allow
            // optimizations to be turned off by prefixing the optimization name with a minus sign.

            Add(Job.Mono.With(new[] { new MonoArgument("--optimize=inline") }).WithId("Inlining enabled"));
            Add(Job.Mono.With(new[] { new MonoArgument("--optimize=-inline") }).WithId("Inlining disabled"));
        }
    }

    [Benchmark]
    public void Sample()
    {
        ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
        ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
        ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
    }

    void ShouldGetInlined() { }
}
```

### Sample results

``` ini
BenchmarkDotNet=v0.10.9.20170903-develop, OS=Windows 10 Redstone 1 (10.0.14393)
Processor=Intel Core i7-6600U CPU 2.60GHz (Skylake), ProcessorCount=4
Frequency=2742190 Hz, Resolution=364.6720 ns, Timer=TSC
  [Host]            : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2101.1
  Inlining disabled : Mono 5.0.1 (Visual Studio), 32bit 
  Inlining enabled  : Mono 5.0.1 (Visual Studio), 32bit 

Runtime=Mono  
```

 | Method |               Job |          Arguments |       Mean |     Error |    StdDev |
 |------- |------------------ |------------------- |-----------:|----------:|----------:|
 | Sample | Inlining disabled | --optimize=-inline | 19.4252 ns | 0.4406 ns | 0.4525 ns |
 | Sample |  Inlining enabled |  --optimize=inline |  0.0000 ns | 0.0000 ns | 0.0000 ns |

## LLVM

To control the `--llvm` and `--no-llvm` options you need to configure Jit property. By default, LLVM is disabled (`--no-llvm`). To enable LLVM (`--llvm`) you just call `Job.Default.With(Jit.Llvm)`