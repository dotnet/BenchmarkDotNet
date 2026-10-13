---
uid: BenchmarkDotNet.Samples.IntroArgumentsSource
---

## Sample: IntroArgumentsSource

In case you want to use a lot of values, you should use
  [`[ArgumentsSource]`](xref:BenchmarkDotNet.Attributes.ArgumentsSourceAttribute).

You can mark one or several fields or properties in your class by the
  [`[ArgumentsSource]`](xref:BenchmarkDotNet.Attributes.ArgumentsSourceAttribute) attribute.
In this attribute, you have to specify the name of public method/property which is going to provide the values
  (something that implements `IEnumerable<T>` or `IAsyncEnumerable<T>`).
The element type has to be named: a source declared to return only the non-generic `IEnumerable` is rejected,
  because the generated code has nothing to infer the argument's type from.

How that element maps onto the benchmark's parameters is read from the element type:

* A benchmark taking **one argument** is fed the value itself, so the source yields the parameter's own type -
  e.g. `IEnumerable<TimeSpan>` for a `TimeSpan` parameter.
* A benchmark taking **multiple arguments** is fed one element per case holding all of them: a `ValueTuple`
  naming each parameter's type - e.g. `IEnumerable<(double x, double y)>`.
* A **by-ref-like** parameter such as `ReadOnlySpan<T>` is reached only through a conversion operator, so the
  source yields the type that operator converts from - e.g. `IEnumerable<byte[]>` for a `ReadOnlySpan<byte>`
  parameter.

> [!NOTE]
> `IEnumerable<object[]>`/`IEnumerable<object>` remain supported for backwards compatibility. However, they are
> no longer recommended. A code fixer is offered to convert such sources to the explicit forms.
>
> The exception is `IEnumerable<object[]>` feeding a benchmark that takes **one** argument, which is fed the
> element itself. A declaration cannot tell a one-element argument list apart from a value that happens to be an
> array, so that shape is refused rather than guessed at; its code fixer unwraps the rows.

The source may be instance or static. If the source is not in the same type as the benchmark, the type containing the source must be specified in the attribute constructor.

A source returning `IAsyncEnumerable<T>` is awaited while the values are read, so they can be produced
  asynchronously without resorting to blocking sync-over-async in the source, and such a source method may take
  an optional [`[EnumeratorCancellation]`](xref:System.Runtime.CompilerServices.EnumeratorCancellationAttribute)
  `CancellationToken` parameter. Starting the run from a thread that carries a single-threaded
  `SynchronizationContext` needs the asynchronous entry points - see
  @BenchmarkDotNet.Samples.IntroParamsSource, where the same applies to `[ParamsSource]`.

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

    public IEnumerable<(SomeClass, SomeStruct)> NonPrimitive()
    {
        yield return (new SomeClass(Enumerable.Range(0, 10).ToArray()), new SomeStruct(10));
        yield return (new SomeClass(Enumerable.Range(0, 15).ToArray()), new SomeStruct(15));
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
