---
uid: BenchmarkDotNet.Samples.IntroTagColumn
---

## Sample: IntroTagColumn

In the following example, we introduce two new columns which contains a tag based on a benchmark method name.

### Source code

[!code-csharp[IntroTagColumn.cs](../../../samples/BenchmarkDotNet.Samples/IntroTagColumn.cs)]

### Output

```markdown
| Method | Mean       | Kind | Number |
| ------ | ---------- | ---- | ------ |
| Bar34  | 10.3636 ms | Bar  | 34     |
| Bar3   | 10.4662 ms | Bar  | 3      |
| Foo12  | 10.1377 ms | Foo  | 12     |
| Foo1   | 10.2814 ms | Foo  | 1      |
```

### Links

* @docs.columns
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroTagColumn

---