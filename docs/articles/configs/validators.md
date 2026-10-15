---
uid: docs.validators
name: Validators
---

# Validators

A **validator** can validate your benchmarks before they are executed and produce validation errors.
If any of the validation errors is critical, then none of the benchmarks will get executed.

Available validators are:

* `BaselineValidator.FailOnError` - it checks if more than 1 Benchmark per class has `Baseline = true` applied. This validator is mandatory.
* `JitOptimizationsValidator.(Dont)FailOnError` - it checks whether any of the referenced assemblies is non-optimized. `DontFailOnError` version is enabled by default.
* `ExecutionValidator.(Dont)FailOnError` - it checks if it is possible to run your benchmarks by executing each of them once. Optional.
* `ReturnValueValidator.(Dont)FailOnError` - it checks if non-void benchmarks return equal values. Optional.
* `NativeAotHostValidator.FailOnError` - when BenchmarkDotNet itself runs inside a Native AOT application, it reports what cannot run there before anything starts: an out-of-process toolchain, `InProcessEmitToolchain`, or a benchmark (or setup/cleanup method) whose result type has no ahead-of-time compiled action. See [Running BenchmarkDotNet inside a NativeAOT application](xref:docs.toolchains#running-benchmarkdotnet-inside-a-nativeaot-application). This validator is mandatory.
