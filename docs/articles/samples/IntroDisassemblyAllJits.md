---
uid: BenchmarkDotNet.Samples.IntroDisassemblyAllJits
---

## Sample: IntroDisassemblyAllJits

You can use a single config to compare the generated assembly code for ALL JITs. 

But to allow benchmarking any target platform architecture the project which defines benchmarks has to target **AnyCPU**. 

```xml
<PropertyGroup>
  <PlatformTarget>AnyCPU</PlatformTarget>
</PropertyGroup>
```

### Source code

[!code-csharp[IntroDisassemblyAllJits.cs](../../../samples/BenchmarkDotNet.Samples/IntroDisassemblyAllJits.cs)]

### Output

The disassembly result can be obtained [here](http://adamsitnik.com/files/disasm/Jit_Devirtualization-disassembly-report.html).
The file was too big to embed it in this doc page.

### See also

* @docs.diagnosers
* @docs.disassembler