---
uid: BenchmarkDotNet.Samples.IntroCustomMono
---

## Sample: IntroCustomMono

BenchmarkDotNet allows you to compare different runtimes, including Mono.
If you apply `[MonoJob]` attribute to your class we use your default mono runtime.
If you want to compare different versions of Mono you need to provide use the custom paths.
You can do this today by using the overloaded ctor of MonoJob attribute or by specifying the runtime in a fluent way.

The mono runtime can also operate as an ahead-of-time compiler. Using mono's AOT mode requires providing the AOT compilation
arguments, as well as the path to mono's corlib. (See IntroCustomMonoObjectStyleAot in the below example).

### Source code

[!code-csharp[IntroCustomMono.cs](../../../samples/BenchmarkDotNet.Samples/IntroCustomMono.cs)]

### Links

* @docs.customizing-runtime
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCustomMono

---