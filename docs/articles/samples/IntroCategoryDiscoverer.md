---
uid: BenchmarkDotNet.Samples.IntroCategoryDiscoverer
---

## Sample: IntroCategoryDiscoverer

The category discovery strategy can be overridden using an instance of `ICategoryDiscoverer`.

### Source code

[!code-csharp[IntroCategoryDiscoverer.cs](../../../samples/BenchmarkDotNet.Samples/IntroCategoryDiscoverer.cs)]

### Output

```markdown
| Method | Categories |     Mean | Error |
|------- |----------- |---------:|------:|
|    Bar |      All,B | 126.5 us |    NA |
|    Foo |      All,F | 114.0 us |    NA |
```

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroCategoryDiscoverer

---