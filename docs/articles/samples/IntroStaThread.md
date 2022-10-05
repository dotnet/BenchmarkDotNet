---
uid: BenchmarkDotNet.Samples.IntroStaThread
---

## Sample: IntroStaThread

If the code you want to benchmark requires `[System.STAThread]`
  then you need to apply this attribute to the benchmarked method.
BenchmarkDotNet will generate executable with `[STAThread]` applied to it's `Main` method. 

Currently it does not work for .NET Core 2.0 due to [this](https://github.com/dotnet/runtime/issues/8834) bug.

### Source code

[!code-csharp[IntroStaThread.cs](../../../samples/BenchmarkDotNet.Samples/IntroStaThread.cs)]

### Links

* @docs.customizing-runtime
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroStaThread

---