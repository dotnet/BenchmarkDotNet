---
uid: BenchmarkDotNet.Samples.IntroCustomEnvironmentInfo
---

## Sample: IntroCustomEnvironmentInfo

You can add any useful information about environment to the benchmark report. Just mark one or several static methods that return `string` or `IEnumerable<string>` by the `[CustomEnvironmentInfo]` attribute.

### Source code

[!code-csharp[IntroCustomEnvironmentInfo.cs](../../../samples/BenchmarkDotNet.Samples/IntroCustomEnvironmentInfo.cs)]

### Output

```markdown
BenchmarkDotNet=v0.10.x-mock, OS=Microsoft Windows NT 10.0.x.mock, VM=Hyper-V
MockIntel Core i7-6700HQ CPU 2.60GHz (Max: 3.10GHz), 1 CPU, 8 logical and 4 physical cores
Frequency=2531248 Hz, Resolution=395.0620 ns, Timer=TSC
Single line
First line from sequence
Second line from sequence
First line from array
Second line from array
  [Host] : Clr 4.0.x.mock, 64mock RyuJIT-v4.6.x.mock CONFIGURATION


 Method |     Mean |    Error |   StdDev |
------- |---------:|---------:|---------:|
    Foo | 202.0 ns | 6.088 ns | 1.581 ns |

Errors: 0
```

### Links

* @docs.configs
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCustomEnvironmentInfo

---