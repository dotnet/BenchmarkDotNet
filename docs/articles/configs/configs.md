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


---

[!include[IntroConfigSource](../samples/IntroConfigSource.md)]

[!include[IntroConfigUnion](../samples/IntroConfigUnion.md)]

[!include[IntroCommandStyle](../samples/IntroCommandStyle.md)]

[!include[IntroFluentConfigBuilder](../samples/IntroFluentConfigBuilder.md)]