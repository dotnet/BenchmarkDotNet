# Encoding
BenchmarkDotNet currently supports two encodings for output - `ASCII` and `Unicode`. By default `ASCII` is setted.
`Unicode` allows to use special characters, like `μ` and `±`. 
*Encoding* allows you to set encoding in your benchmark.

## Usage
There are few ways to set encoding in benchmark:

### Object style

```cs
[Config(typeof(Config))]
public class MyClassWithBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(new Job1(), new Job2());
            Add(new Column1(), new Column2());
            Add(new Exporter1(), new Exporter2());
            Add(new Logger1(), new Logger2());
            Add(new Diagnoser1(), new Diagnoser2());
            Add(new Analyser1(), new Analyser2());
            Set(Encoding.Unicode);
        }
    }
    
    [Benchmark]
    public void Benchmark1()
    {
    }
}
```

### Attribute

```cs
[EncodingAttribute.Unicode]
public class IntroConfigEncoding
{
    private const int N = 1002;
    private readonly ulong[] numbers;
    private readonly Random random = new Random(42);

    public IntroConfigEncoding()
    {
        numbers = new ulong[N];
        for (int i = 0; i < N; i++)
        numbers[i] = NextUInt64();
    }

    public ulong NextUInt64()
    {
        var buffer = new byte[sizeof(long)];
        random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    [Benchmark]
    public double Foo()
    {
        int counter = 0;
        for (int i = 0; i < N / 2; i++)
            counter += BitCountHelper.PopCountParallel2(numbers[i],numbers[i+1]);
        return counter;
    }
}
```
### Fluent Config
```cs
BenchmarkRunner.Run<IntroConfigEncoding>(ManualConfig
               .Create(DefaultConfig.Instance)
               .With(Job.RyuJitX64)
               .With(Job.Core)
               .With(ExecutionValidator.FailOnError)
               .With(Encoding.Unicode));
```

## Be adviced!
You should be sure that your terminal/text editor supports Unicode. On Windows, you may have some troubles with Unicode symbols if system default code page configured as non-English (in Control Panel + Regional and Language Options, Language for Non-Unicode Programs).