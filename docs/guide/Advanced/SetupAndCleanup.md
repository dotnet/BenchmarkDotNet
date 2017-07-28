# Setup And Cleanup

Sometimes we want to write some logic which should be executed *before* or *after* a benchmark, but we don't want to measure it.
For this purpose, BenchmarkDotNet provides a set of attributes: `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]`.

## GlobalSetup

A method which is marked by the `[GlobalSetup]` attribute will be executed only once per a benchmarked method
  after initialization of benchmark parameters and before all the benchmark method invocations.

```cs
public class GlobalSetupExample
{
    [Params(10, 100, 1000)]
    public int N;

    private int[] data;

    [GlobalSetup]
    public void GlobalSetup()
    {
        data = new int[N]; // executed once per each N value
    }

    [Benchmark]
    public int Logic()
    {
        int res = 0;
        for (int i = 0; i < N; i++)
            res += data[i];
        return res;
    }
}
```

## GlobalCleanup

A method which is marked by the `[GlobalCleanup]` attribute will be executed only once per a benchmarked method
  after all the benchmark method invocations.
If you are using some unmanaged resources (e.g., which were created in the `GlobalSetup` method), they can be disposed in the `GlobalCleanup` method.

```cs
public void GlobalCleanup()
{
    // Disposing logic
}
```

## IterationSetup

A method which is marked by the `[IterationSetup]` attribute will be executed only once *before each an iteration*.
It's not recommended to use this attribute in microbenchmarks because it can spoil the results.
However, if you are writing a macrobenchmark (e.g. a benchmark which takes at least 100ms) and
  you want to prepare some data before each iteration, `[IterationSetup]` can be useful.
BenchmarkDotNet doesn't support setup/cleanup method for a single method invocation (*an operation*), but you can perform only one operation per iteration.
It's recommended to use `RunStrategy.Monitoring` for such cases.
Be careful: if you allocate any objects in the `[IterationSetup]` method, the MemoryDiagnoser results can also be spoiled.

## IterationCleanup
A method which is marked by the `[IterationCleanup]` attribute will be executed only once *after each an iteration*.
This attribute has the same set of constraint with `[IterationSetup]`: it's not recommended to use `[IterationCleanup]` in microbenchmarks or benchmark which also 

## An example

```cs
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, targetCount: 3)]
public class SetupAndCleanupExample
{
  private int setupCounter;
  private int cleanupCounter;

  [IterationSetup]
  public void IterationSetup() => Console.WriteLine("// " + "IterationSetup" + " (" + ++setupCounter + ")");

  [IterationCleanup]
  public void IterationCleanup() => Console.WriteLine("// " + "IterationCleanup" + " (" + ++cleanupCounter + ")");

  [GlobalSetup]
  public void GlobalSetup() => Console.WriteLine("// " + "GlobalSetup");

  [GlobalCleanup]
  public void GlobalCleanup() => Console.WriteLine("// " + "GlobalCleanup");

  [Benchmark]
  public void Benchmark() => Console.WriteLine("// " + "Benchmark");
}
```

The order of method calls:

```
// GlobalSetup

// IterationSetup (1)    // IterationSetup Jitting
// IterationCleanup (1)  // IterationCleanup Jitting

// IterationSetup (2)    // MainWarmup1
// Benchmark             // MainWarmup1
// IterationCleanup (2)  // MainWarmup1

// IterationSetup (3)    // MainWarmup2
// Benchmark             // MainWarmup2
// IterationCleanup (3)  // MainWarmup2

// IterationSetup (4)    // MainTarget1
// Benchmark             // MainTarget1
// IterationCleanup (4)  // MainTarget1

// IterationSetup (5)    // MainTarget2
// Benchmark             // MainTarget2
// IterationCleanup (5)  // MainTarget2

// IterationSetup (6)    // MainTarget3
// Benchmark             // MainTarget3
// IterationCleanup (6)  // MainTarget3

// GlobalCleanup
```

## Target 

Sometimes it's useful to run setup or cleanups for specific benchmarks. All four setup and cleanup attributes have a Target property that allow the setup/cleanup method to be run for one or more specific benchmark methods.   

```cs
[SimpleJob(RunStrategy.Monitoring, launchCount: 0, warmupCount: 0, targetCount: 1)]
public class SetupAndCleanupExample
{
  [GlobalSetup(Target = nameof(BenchmarkA))]
  public void GlobalSetupA() => Console.WriteLine("// " + "GlobalSetup A");
  
  [Benchmark]
  public void BenchmarkA() => Console.WriteLine("// " + "Benchmark A");
  
  [GlobalSetup(Target = nameof(BenchmarkB) + "," + nameof(BenchmarkC))]
  public void GlobalSetupB() => Console.WriteLine("// " + "GlobalSetup B");
  
  [Benchmark]
  public void BenchmarkB() => Console.WriteLine("// " + "Benchmark B");
  
  [Benchmark]
  public void BenchmarkC() => Console.WriteLine("// " + "Benchmark C");
  
  [Benchmark]
  public void BenchmarkD() => Console.WriteLine("// " + "Benchmark D");
}
```

The order of method calls:

```
// GlobalSetup A

// Benchmark A

// GlobalSetup B

// Benchmark B

// GlobalSetup B

// Benchmark C

// Benchmark D
```
