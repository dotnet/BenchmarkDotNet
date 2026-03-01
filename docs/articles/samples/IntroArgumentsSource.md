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
The source may be instance or static. If the source is not in the same type as the benchmark, the type containing the source must be specified in the attribute constructor.

### Source code

[!code-csharp[IntroArgumentsSource.cs](../../../samples/BenchmarkDotNet.Samples/IntroArgumentsSource.cs)]

### Output

```markdown
| Method         | time              |  x |  y |            Mean |          Error |         StdDev |
|--------------- |------------------ |--- |--- |----------------:|---------------:|---------------:|
| SingleArgument | 00:00:00.0100000 |  ? |  ? |  15,780,658.9 ns |   53,493.3 ns |   50,037.7 ns |
| SingleArgument | 00:00:00.1000000 |  ? |  ? | 110,181,308.0 ns |  517,614.4 ns |  484,176.8 ns |
| ManyArguments  |                ? |  1 |  1 |           3.135 ns |       0.0852 ns |       0.1326 ns |
| ManyArguments  |                ? |  2 |  2 |          13.571 ns |       0.2180 ns |       0.1933 ns |
| ManyArguments  |                ? |  4 |  4 |          13.478 ns |       0.2188 ns |       0.1940 ns |
| ManyArguments  |                ? | 10 | 10 |          13.471 ns |       0.2294 ns |       0.2034 ns |
```

> `?` is displayed when a column is not applicable for the given benchmark (e.g., `x`/`y` for `SingleArgument`, `time` for `ManyArguments`).

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
