---
uid: docs.configs
name: Configs
---

# Configs

Config is a set of so called `jobs`, `columns`, `exporters`, `loggers`, `diagnosers`, `analysers`, `validators`
  that help you to build your benchmark. 

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
            AddJob(new Job1(), new Job2());
            AddColumn(new Column1(), new Column2());
            AddColumnProvider(new ColumnProvider1(), new ColumnProvider2());
            AddExporter(new Exporter1(), new Exporter2());
            AddLogger(new Logger1(), new Logger2());
            AddDiagnoser(new Diagnoser1(), new Diagnoser2());
            AddAnalyser(new Analyser1(), new Analyser2());
            AddValidator(new Validator2(),new Validator2());
            AddHardwareCounters(HardwareCounter enum1, HardwareCounter enum2);
            AddFilter(new Filter1(), new Filter2());
            AddLogicalGroupRules(BenchmarkLogicalGroupRule enum1, BenchmarkLogicalGroupRule enum2);
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


---

[!include[IntroConfigSource](../samples/IntroConfigSource.md)]

[!include[IntroConfigUnion](../samples/IntroConfigUnion.md)]

[!include[IntroFluentConfigBuilder](../samples/IntroFluentConfigBuilder.md)]