---
uid: BenchmarkDotNet.Samples.IntroParamsAllValues
---

## Sample: IntroParamsAllValues

If you want to use all possible values of an `enum` or another type with a small number of values, you can use the [`[ParamsAllValues]`](xref:BenchmarkDotNet.Attributes.ParamsAllValuesAttribute) attribute, instead of listing all the values by hand. The types supported by the attribute are:

* `bool`
* any `enum` that is not marked with `[Flags]`
* `Nullable<T>`, where `T` is an enum or boolean

### Source code

[!code-csharp[IntroParamsAllValues.cs](../../../samples/BenchmarkDotNet.Samples/IntroParamsAllValues.cs)]

### Output

```markdown
    Method |     E |     B |     Mean | Error |
---------- |------ |------ |---------:|------:|
 Benchmark |   One |     ? | 101.4 ms |    NA |
 Benchmark |   One | False | 111.1 ms |    NA |
 Benchmark |   One |  True | 122.0 ms |    NA |
 Benchmark |   Two |     ? | 201.3 ms |    NA |
 Benchmark |   Two | False | 212.1 ms |    NA |
 Benchmark |   Two |  True | 221.3 ms |    NA |
 Benchmark | Three |     ? | 301.4 ms |    NA |
 Benchmark | Three | False | 311.5 ms |    NA |
 Benchmark | Three |  True | 320.8 ms |    NA |
```

### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroParamsAllValues

---