---
uid: BenchmarkDotNet.Samples.IntroVisualStudioProfiler
---

## Sample: Visual Studio Profiler

Using the [Microsoft.VisualStudio.BenchmarkDotNetDiagnosers](https://www.nuget.org/packages/Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers) NuGet package you can capture performance profiles of your benchmarks that can be opened in Visual Studio. 

### Source code

[!code-csharp[IntroVisualStudioDiagnoser.cs](../../../samples/BenchmarkDotNet.Samples/IntroVisualStudioDiagnoser.cs)]

### Output
The output will contain a path to the collected diagsession and automatically open in Visual Studio when launched from it.

```markdown
// * Diagnostic Output - VSDiagnosticsDiagnoser *
Collection result moved to 'C:\Work\BenchmarkDotNet\samples\BenchmarkDotNet.Samples\bin\Release\net8.0\BenchmarkDotNet.Artifacts\BenchmarkDotNet_IntroVisualStudioProfiler_20241205_192056.diagsession'.
Session : {d54ebddb-2d6d-404f-b1da-10acbc89635f}
  Stopped
Exported diagsession file: C:\Work\BenchmarkDotNet\samples\BenchmarkDotNet.Samples\bin\Release\net8.0\BenchmarkDotNet.Artifacts\BenchmarkDotNet_IntroVisualStudioProfiler_20241205_192056.diagsession.
Opening diagsession in VisualStudio: 15296
```