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
