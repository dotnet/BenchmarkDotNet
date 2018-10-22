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
    Method |   E |     B |     Mean | Error |
---------- |---- |------ |---------:|------:|
 Benchmark |   A |     ? | 101.9 ms |    NA |
 Benchmark |   A | False | 111.9 ms |    NA |
 Benchmark |   A |  True | 122.3 ms |    NA |
 Benchmark |  BB |     ? | 201.5 ms |    NA |
 Benchmark |  BB | False | 211.8 ms |    NA |
 Benchmark |  BB |  True | 221.4 ms |    NA |
 Benchmark | CCC |     ? | 301.8 ms |    NA |
 Benchmark | CCC | False | 312.3 ms |    NA |
 Benchmark | CCC |  True | 322.2 ms |    NA |

// * Legends *
  E     : Value of the 'E' parameter
  B     : Value of the 'B' parameter
```

### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroParamsAllValues

---