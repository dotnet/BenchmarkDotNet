---
uid: BenchmarkDotNet.Samples.IntroEnvVars
---

## Sample: IntroEnvVars

You can configure custom environment variables for the process that is running your benchmarks.
One reason for doing this might be checking out how different
  [runtime knobs](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/clr-configuration-knobs.md)
  affect the performance of .NET Core.

### Source code

[!code-csharp[IntroEnvVars.cs](../../../samples/BenchmarkDotNet.Samples/IntroEnvVars.cs)]

### See also

* @docs.customizing-runtime
* @docs.configs
* @docs.jobs