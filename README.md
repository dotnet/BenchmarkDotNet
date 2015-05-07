**BenchmarkDotNet** is a lightweight .NET library for benchmarking. You can install BenchmarkDotNet via [NuGet package](https://www.nuget.org/packages/BenchmarkDotNet/).

## Features
* BenchmarkDotNet creates an isolated project for each benchmark method and run it in a separate runtime in the Release mode without an attached debugger.
* You can create benchmark tasks for running your benchmark with different CLR version, JIT version, platform version, and so on.
* BenchmarkDotNet makes warmup of your code, then runs it several times, calculates statistic, and tries to eliminate some runtime side-effects.
* BenchmarkDotNet almost eliminate own performance overhead.

## An example

Source:

```cs
[Task(mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X86)]
[Task(mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X64)]
[Task(mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X64, 
      jitVersion: BenchmarkJitVersion.RyuJit)]
public class Cpu_InstructionLevelParallelism
{
    private const int IterationCount = 400000001;

    private readonly int[] a = new int[4];

    [Benchmark]
    public int[] Parallel()
    {
        for (int iteration = 0; iteration < IterationCount; iteration++)
        {
            a[0]++;
            a[1]++;
            a[2]++;
            a[3]++;
        }
        return a;
    }

    [Benchmark]
    public int[] Sequential()
    {
        for (int iteration = 0; iteration < IterationCount; iteration++)
            a[0]++;
        for (int iteration = 0; iteration < IterationCount; iteration++)
            a[1]++;
        for (int iteration = 0; iteration < IterationCount; iteration++)
            a[2]++;
        for (int iteration = 0; iteration < IterationCount; iteration++)
            a[3]++;
        return a;
    }
}

// You can run both x86 and x64 benchmark tasks only from AnyCPU application
new BenchmarkRunner().RunCompetition(new Cpu_InstructionLevelParallelism());
```

Result:

	// BenchmarkDotNet=v0.7.0.0
	// OS=Microsoft Windows NT 6.2.9200.0
	// Processor=Intel(R) Core(TM) i7-4702MQ CPU @ 2.20GHz, ProcessorCount=8
	Common:  Type=Cpu_InstructionLevelParallelism  Mode=SingleRun  .NET=V40
	
	     Method | Platform |       Jit |  Value | StdDev |
	----------- |--------- |---------- |------- |------- |
	   Parallel |      X64 | LegacyJit |  843ms |    1ms |
	 Sequential |      X64 | LegacyJit | 3913ms |    4ms |
	   Parallel |      X64 |    RyuJit |  994ms |    2ms |
	 Sequential |      X64 |    RyuJit | 3391ms |    4ms |
	   Parallel |      X86 | LegacyJit | 1444ms |    3ms |
	 Sequential |      X86 | LegacyJit | 4171ms |   25ms |