---
uid: BenchmarkDotNet.Samples.IntroCustomMonoArguments
---

## Sample: IntroCustomMonoArguments


### Source code

[!code-csharp[IntroCustomMonoArguments.cs](../../../samples/BenchmarkDotNet.Samples/IntroCustomMonoArguments.cs)]

### Output

```markdown
| Method |               Job |          Arguments |       Mean |    StdDev |
|------- |------------------ |------------------- |-----------:|----------:|
| Sample | Inlining disabled | --optimize=-inline | 19.4252 ns | 0.4525 ns |
| Sample |  Inlining enabled |  --optimize=inline |  0.0000 ns | 0.0000 ns |
```

### Links

* @docs.customizing-runtime
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCustomMonoArguments

---