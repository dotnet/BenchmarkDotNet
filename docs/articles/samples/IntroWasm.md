---
uid: BenchmarkDotNet.Samples.IntroWasm
---

## Sample: IntroWasm

`WasmToolchain` builds benchmarks as WebAssembly and runs them under a JavaScript engine (V8 by default).

It is supported only on Unix.

If you hit `NETSDK1147` (missing workload), install the required workload (for example: `dotnet workload install wasm-tools`).

### Source code

[!code-csharp[IntroWasm.cs](../../../samples/BenchmarkDotNet.Samples/IntroWasm.cs)]

### Links

* @docs.toolchains
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroWasm

---
