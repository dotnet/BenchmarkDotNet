---
uid: BenchmarkDotNet.Samples.IntroArgumentsSource
---

## Sample: IntroArgumentsSource

In case you want to use a lot of values, you should use
  [`[ArgumentsSource]`](xref:BenchmarkDotNet.Attributes.ArgumentsSourceAttribute).

You can mark one or several fields or properties in your class by the
  [`[ArgumentsSource]`](xref:BenchmarkDotNet.Attributes.ArgumentsSourceAttribute) attribute.
In this attribute, you have to specify the name of public method/property which is going to provide the values
  (something that implements `IEnumerable`).
 The source must be within benchmarked type! 

### Source code

[!code-csharp[IntroArgumentsSource.cs](../../../samples/BenchmarkDotNet.Samples/IntroArgumentsSource.cs)]

### Output

```markdown
| Method |  x |  y |      Mean |     Error |    StdDev |
|------- |--- |--- |----------:|----------:|----------:|
|    Pow |  1 |  1 |  9.360 ns | 0.0190 ns | 0.0149 ns |
|    Pow |  2 |  2 | 40.624 ns | 0.3413 ns | 0.3192 ns |
|    Pow |  4 |  4 | 40.537 ns | 0.0560 ns | 0.0524 ns |
|    Pow | 10 | 10 | 40.395 ns | 0.3274 ns | 0.3063 ns |
```

### Another example

If the values are complex types you need to override `ToString` method to change the display names used in the results.

```cs
[DryJob]
public class WithNonPrimitiveArgumentsSource
{
    [Benchmark]
    [ArgumentsSource(nameof(NonPrimitive))]
    public void Simple(SomeClass someClass, SomeStruct someStruct)
    {
        for (int i = 0; i < someStruct.RangeEnd; i++)
            Console.WriteLine($"// array.Values[{i}] = {someClass.Values[i]}");
    }

    public IEnumerable<object[]> NonPrimitive()
    {
        yield return new object[] { new SomeClass(Enumerable.Range(0, 10).ToArray()), new SomeStruct(10) };
        yield return new object[] { new SomeClass(Enumerable.Range(0, 15).ToArray()), new SomeStruct(15) };
    }

    public class SomeClass
    {
        public SomeClass(int[] initialValues) => Values = initialValues.Select(val => val * 2).ToArray();

        public int[] Values { get; }

        public override string ToString() => $"{Values.Length} items";
    }

    public struct SomeStruct
    {
        public SomeStruct(int rangeEnd) => RangeEnd = rangeEnd;

        public int RangeEnd { get; }

        public override string ToString() => $"{RangeEnd}";
    }
}
```

```markdown
| Method | someClass | someStruct |     Mean | Error |
|------- |---------- |----------- |---------:|------:|
| Simple |  10 items |         10 | 887.2 us |    NA |
| Simple |  15 items |         15 | 963.1 us |    NA |
```


### Links

* @docs.parameterization
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroArgumentsSource

---