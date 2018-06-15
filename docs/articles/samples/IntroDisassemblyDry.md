---
uid: BenchmarkDotNet.Samples.IntroDisassemblyDry
---

## Sample: IntroDisassemblyDry

**Getting only the Disassembly without running the benchmarks for a long time.**

Sometimes you might be interested only in the disassembly, not the results of the benchmarks.
In that case you can use **Job.Dry** which runs the benchmark only **once**.

### Source code

[!code-csharp[IntroDisassemblyDry.cs](../../../samples/BenchmarkDotNet.Samples/IntroDisassemblyDry.cs)]

### Links

* @docs.diagnosers
* @docs.disassembler
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroDisassemblyDry

---