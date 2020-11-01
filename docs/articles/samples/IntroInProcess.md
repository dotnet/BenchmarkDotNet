---
uid: BenchmarkDotNet.Samples.IntroInProcess
---

## Sample: IntroInProcess

InProcessEmitToolchain is our toolchain which does not generate any new executable.
It emits IL on the fly and runs it from within the process itself.
It can be useful if want to run the benchmarks very fast or if you want to run them for framework which we don't support.
An example could be a local build of CoreCLR.

### Usage

```cs
[InProcessAttribute]
public class TypeWithBenchmarks
{
}
```


### Source code

[!code-csharp[IntroInProcess.cs](../../../samples/BenchmarkDotNet.Samples/IntroInProcess.cs)]

### Output


### Links

* @docs.toolchains
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroInProcess

---