# Configs

Config is a set of so called `jobs`, `columns`, `exporters`, `loggers`, `diagnosers`, `analysers`, `validators` that help you to build your benchmark. 

## Built-in configuration
There are two built-in ways to set your config:

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
            Add(new Filter1(), new Filter2());
        }
    }
    
    [Benchmark]
    public void Benchmark1()
    {
    }
    
    [Benchmark]
    public void Benchmark2()
    {
    }
}
```

### Command style

```cs
[Config("jobs=job1,job2 " +
        "columns=column1,column2 " +
        "exporters=exporter1,exporter2 " +
        "loggers=logger1,logger2 " +
        "diagnosers=diagnoser1,diagnoser2 " +
        "analysers=analyser1,analyser2")]
public class MyClassWithBenchmarks
{
    [Benchmark]
    public void Benchmark1()
    {
    }
    
    [Benchmark]
    public void Benchmark2()
    {
    }
}
```

## Custom configs

You can also define your own way to specify config parameters. 

### Custom attribute

You can define own config attribute:

```cs
[MyConfigSource(Jit.LegacyJit, Jit.RyuJit)]
public class IntroConfigSource
{
    private class MyConfigSourceAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; private set; }

        public MyConfigSourceAttribute(params Jit[] jits)
        {
            var jobs = jits.Select(jit => new Job(Job.Dry) { Env = { Platform = Platform.X64, Jit = jit } }).ToArray();
            Config = ManualConfig.CreateEmpty().With(jobs);
        }
    }

    [Benchmark]
    public void Foo()
    {
        Thread.Sleep(10);
    }
}
```

### Fluent config

There is no need to create new Config type, you can simply use fluent interface:

```cs
BenchmarkRunner
    .Run<Algo_Md5VsSha256>(
        ManualConfig
            .Create(DefaultConfig.Instance)
            .With(Job.RyuJitX64)
            .With(Job.Core)
            .With(ExecutionValidator.FailOnError));
```
