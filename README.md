**BenchmarkDotNet** is a lightweight .NET library for benchmarking. You can install BenchmarkDotNet via [NuGet package](https://www.nuget.org/packages/BenchmarkDotNet/).

## Content

* [Features](#features)
* [Why is microbenchmarking hard?](#why-is-microbenchmarking-hard)
* [Microbenchmarking rules](#microbenchmarking-rules)
* [Roadmap](#roadmap)
* [Usage example](#usage-example)

<a name="features"></a>
## Features
* BenchmarkDotNet creates an isolated project for each benchmark method and run it in a separate runtime in the Release mode without an attached debugger.
* You can create benchmark tasks that run your benchmark with different CLR version, JIT version, platform version, and so on.
* BenchmarkDotNet makes warmup of your code, then runs it several times, calculates statistic, and tries to eliminate some runtime side-effects.
* BenchmarkDotNet almost eliminate own performance overhead.

<a name="why-is-microbenchmarking-hard"></a>
## Why is microbenchmarking hard?

Indeed, microbencmarking is very hard. In this section, we observe some BenchmarkDotNet approaches that allow you to make the benchmark experiments honest.

### Separate CLR instances

The best way to make a competition of several benchmark mathods is run it using difference CLR instances. In the other case, you can have some troubles.

* For example, you want to measure the following methods:

```cs
interface IFoo
{
  int Inc(int x);
}
class Foo1 : IFoo
{
  public int Inc(int x)
  {
    return x + 1;
  }
}
class Foo2 : IFoo
{
  public int Inc(int x)
  {
    return x + 1;
  }
}
void Run(IFoo foo)
{
  for (int i = 0; i < 1000001; i++)
    foo.Inc(0);
}
void Run1()
{
  Run(new Foo1());    
}
void Run2()
{
  Run(new Foo2());    
}
// Target benchmark methods: Run1, Run2
```

If you measure `Run1` and then `Run2`, the results may differ. In the first case, there is a single implementation of `IFoo` in the memory. So, JIT can optimize the invocation of the `Inc` method. In the next case, there are two implementation of `IFoo` and the such optimization is impossible. If you measure the target method in a common CLR instance one by one, the results of the competition can be wrong for some JIT implementation.

* It may seems that the application domains is the solution. Indeed, we can run each benchmark method in a separate application domain. But it is not real solution in the general case. Remember that all of the app domains [share](http://stackoverflow.com/questions/15246167/does-garbage-collection-happen-at-the-process-level-or-appdomain-level) the heap and use common GC. Next, remember that the MS.NET GC is [self-tuning](https://msdn.microsoft.com/en-us/library/ee787088.aspx). So, the execution of the first benchmark method in the first app domain can affect to GC and to execution of the second benchmark method in the second app domain.

Thus, the best choise for benchmarking is run each target method in own CLR instance.

### Target method invocation

* **Wrong approach 1.** Let's assume that we want to compare performance of two methods:

```cs
void Foo1()
{
  // ...
}
void Foo2()
{
  // ...
}
```

Maybe you want to write the code like the following:

```cs
// Measure: start
for (int i = 0; i < IterationCount; i++)
  Foo1();
// Measure: end
// Measure: start
for (int i = 0; i < IterationCount; i++)
  Foo2();
// Measure: end
```

In this case, we can have a major issue: one method can be inlined and other method can be not inlined. And it will greatly affect to the benchmark results. You can't predict whether it's going to happen. Moreover, different JIT version can perform inlining in different cases. For example, let's consider the following method:

```cs
int WithStarg(int value)
{
    if (value < 0)
        value = -value;
    return value;
}
```

This method contains the `starg` IL opcode and JIT-x86 [can't inline it](http://aakinshin.net/en/blog/dotnet/inlining-and-starg/), but JIT-x64 can.

* **Wrong approach 2.** Let's talk about code generation. What if we take the following method:

```cs
double Foo()
{
    return /* target expression */;
}
```

and instead of benchmark like this

```
double accumulator = 0;
for (int i = 0; i < IterationCount; i++)
  accumulator += Foo();
```

we will automatically generate a wrapper like the following:

```cs
double accumulator = 0;
for (int i = 0; i < IterationCount; i++)
  accumulator += /* target expression */;
```

It is wrong approach too because these scenarios are not equivalent because of [CPU instruction-level parallelism](http://en.wikipedia.org/wiki/Instruction-level_parallelism). If we perform explicit inlining, the CPU can apply additional optimizations and spoil the pure result for single operation.

* **BenchmarkDotNet approach**. BenchmarkDotNet creates a delegate for each target method and invoke it. The great fact about delegates: JIT can't inline it. Of course, we have some overhead because of delegates invocation, but BenchmarkDotNet tries to eliminate it.

### Warmup and Target runs

* If you run your benchmark at the first time, it is so called the cold start. It includes big amount of third-party logic: jitting of target methods, assemblies loading, CPU cache warmup, and so on. All of that can increase the work time and spoil the benchmark results. Thus, first of all, you should make warmup: run your benchmark several times idles. Only then you can perform target runs and measure its time.

* Also you can perform several target runs: results may vary from time to time. At the end, you should take the average time.

* Another good practice is run the target CLR instance several times and collect measurements of target runs in each instance. This will improve the quality of your benchmarks.

* Statistics is important. You should calculate at least min, max, and standard deviation of your target runs measurements. If the standard deviation is big, you shouldn't use only the average time as a result. Maybe you have some mistakes in your benchmark method or the measured operation doesn't have permanent work time.

### Different environments

There are big amount of different environments for your .NET program. You can use the x86 platform or the x64 platform. You can use the legacy jit or new modern RyuJIT. You can use different target frameworks or CLR versions. You can run your benchmark with classic Microsoft .NET Framework or use Mono or CoreCLR. Don't extrapolate benchmark results for single environment to general behaviour. For example, if you switch legacy JIT-x64 to RyuJIT (it is also x64 for now; .NET Framework 4.6 includes RyuJIT by default), it can significantly affect the results. LegacyJit-x64 and RyuJIT use different logic for performing big amount of optimizations: inlining, array bound check elimination, loop unrolling, and so on. Implementations of BCL classes may also differ. For example, there are [two different](http://blogs.msdn.com/b/jankrivanek/archive/2012/11/30/stringbuilder-performance-issues-in-net-4-0-and-4-5.aspx) implementation of StringBuilder: .NET Framework 2.0 StringBuilder and .NET Framework 4.0 StringBuilder. These implementation has different operation complexity by design. 

### Loop unrolling

Beware of loops inside the target method. For example, let's consider the following code:

```cs
for (int i = 0; i < 1000; i++)
    Foo();
```

LegacyJIT-x64 will perform [loop unrolling](http://en.wikipedia.org/wiki/Loop_unrolling) and transform the code to the following:

```cs
for (int i = 0; i < 1000; i += 4)
{
    Foo();
    Foo();
    Foo();
    Foo();
}
```

For now, LegacyJIT-x86 and RyuJIT can't do it. Such loop unrolling can also spoil the measurement of the `Foo()` invocation.

### GC

You should control GC overhead and collect the garbage between measurements. The target method shouldn't create objects which can't be collected. A sudden GC stop-the-world can increase time of the target runs. 

### Right measuring instrument

You shouldn't use [DateTime](https://msdn.microsoft.com/library/system.datetime.aspx) for measure your benchmark, it gives you bad poor precision. The best choise is [Stopwatch](https://msdn.microsoft.com/library/system.diagnostics.stopwatch.aspx).

### Sufficient measuring time

If you measure the target method during 1–2 ms, such benchmark doesn't show anything. In this case, influence of runtime and hardware is too big, it is spoils all the measurements. If you want to do a microbenchmark, you should run the target method several times (at least 1 second per all invocation) and calculate average time.

### ProcessorAffinity

For now, BenchmarkDotNet allows you to make only the single thread benchmarks. Multithreading benchmarking is very a hard job, but future plans includes support such kind of benchmarks. Even the single thread benchmarking is the a hard job. For example, you process can be moved from one CPU core to another with a cold processor cache. In this case, results of the measurement will be spoiled. Because of that, BenchmarkDotNet set [ProcessortAffinity](https://msdn.microsoft.com/en-us/library/system.diagnostics.process.processoraffinity.aspx) of the process.  
	
### Benchmark infrastructure overhead

However, if you try to measure something like this:

```cs
for (int i = 0; i < IterationCount; i++)
    Foo();
```

you will actually measure not only the `Foo()` time, but the `Foo()` time plus the `for` time plus the `Foo()` invocations time. It is critical in microbenchmarking. So, you should try to eliminate overhead of your benchmark infrostructure. Fortunately, BenchmarkDotNet tries to do it as much as possible.

### Conclusion

Thus, hand-writing of the benchmark infrastucture for each benchmark is very hard. Therefore it is best to use a special benchmark library (e.g., *BenchmarkDotNet*) for your experiments.

<a name="microbenchmarking-rules"></a>
## Microbenchmarking rules.

Even if you use the BenchmarkDotNet library for benchmarking, there are some rules that you should follow.

### Use the Release build without an attached debugger

Never use the Debug build for benchmarking. *Never*. The debug version of the target method can run 10 times slower. The release mode means that you should have `<Optimize>true</Optimize>` in your csproj file or use [/optimize](https://msdn.microsoft.com/en-us/library/t0hfscdc.aspx) for `csc`. Also your never should use an attached debugger (e.g. Visual Studio or WinDbg) during the benchmarking. The best way is build our benchmark in the Release mode and run it with `cmd`.

### Try different environments

I remind you again: the results in different environments may vary significantly. If a `Foo1` method is faster than a `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows, it means that the `Foo1` method is faster than the `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows and nothing else. And you can not say anything about methods performance for CLR 2 or .NET Framework 4.6 or LegacyJIT-x64 or x86 or Linux+Mono until you try it. 

### Avoid dead code elimination

You should also use the result of calculation. For example, if you run the following code:

```cs
void Foo()
{
    Math.Exp(1);
}
```

then JIT can eliminate this code because the result of `Math.Exp` is not used. The better way is use it like this:

```cs
double Foo()
{
    return Math.Exp(1);
}
```

### Minimize work with memory

If you don't measure efficiency of access to memory, efficiency of the CPU cache, efficiency of GC, you shouldn't create big arrays and you shouldn't allocate big amount of memory. For example, you want to measure performance of `ConvertAll(x => 2 * x).ToList()`. You can write code like this:

```cs
List<int> list = /* ??? */;
public List<int> ConvertAll()
{
    return list.ConvertAll(x => 2 * x).ToList();
}
```

In this case, you should create a small list like this:

```cs
List<int> list = new List<int> { 1, 2, 3, 4, 5 };
```

If you create a big list (with millions of elements), then you will also measure efficiency of the CPU cache because you will have big amount of [cache miss](http://en.wikipedia.org/wiki/CPU_cache#Cache_miss) during the calculation.  

### Power settings and other applications

* Turn off all of the applications except the benchmark process and the standard OS processes. If you run benchmark and work in the Visual Studio at the same time, it can negatively affect to benchmark results.
* If you use laptop for benchmarking, keep it plugged in and use the maximum performance mode. 

<a name="roadmap"></a>
## Roadmap

Some plans for the future development:

* Multithreading benchmarking
* Support of Mono and CoreCLR
* Automatic warmup
* Plots with results
* Hardware analysis

<a name="usage-example"></a>
## Usage example

In the following example, we will research how [Instruction-level parallelism](http://en.wikipedia.org/wiki/Instruction-level_parallelism) affects to the application performance:

```cs
[Task(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
[Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
[Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
public class Cpu_Ilp_Inc
{
    private double a, b, c, d;

    [Benchmark]
    [OperationCount(4)]
    public void Parallel()
    {
        a++;
        b++;
        c++;
        d++;
    }

    [Benchmark]
    [OperationCount(4)]
    public void Sequential()
    {
        a++;
        a++;
        a++;
        a++;
    }
}
```

An example of results:

```
// BenchmarkDotNet=v0.7.4.0
// OS=Microsoft Windows NT 6.2.9200.0
// Processor=Intel(R) Core(TM) i7-4702MQ CPU @ 2.20GHz, ProcessorCount=8
// CLR=MS.NET 4.0.30319.0, Arch=64-bit  [RyuJIT]
Common:  Type=Cpu_Ilp_Inc  Mode=Throughput  .NET=Current

     Method | Platform |       Jit |  AvrTime |     StdDev |          op/s |
----------- |--------- |---------- |--------- |----------- |-------------- |
   Parallel |      X64 | LegacyJit | 0.443 ns |  0.0144 ns | 2255130155.43 |
 Sequential |      X64 | LegacyJit |  2.62 ns | 0.00877 ns |  381202467.78 |
   Parallel |      X64 |    RyuJit | 0.630 ns | 0.00436 ns | 1587486530.09 |
 Sequential |      X64 |    RyuJit |  1.05 ns | 0.00486 ns |  949123537.96 |
   Parallel |      X86 | LegacyJit | 0.562 ns |  0.0206 ns | 1777793672.86 |
 Sequential |      X86 | LegacyJit |  2.96 ns |  0.0197 ns |  337625645.72 |
```

You can find more examples in the [BenchmarkDotNet.Samples](https://github.com/AndreyAkinshin/BenchmarkDotNet/tree/master/BenchmarkDotNet.Samples) project.