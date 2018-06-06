# Arguments

As an alternative to using `[Params]`, you can specify arguments for your benchmarks. There are several ways to do it (described below). 

**Important:** InProcessToolchain does not support Arguments (yet!). See [#687](https://github.com/dotnet/BenchmarkDotNet/issues/687) for more details.

## \[Arguments\]

The \[ArgumentsAttribute\] allows you to provide a set of values. Every value must be a compile-time constant (it's C# lanugage limitation for attributes in general).
You can also combine `Arguments` with `Params`. As a result, you will get results for each combination of params values.


```cs
public class IntroArguments
{
    [Params(true, false)] // Arguments can be combined with Params
    public bool AddExtra5Miliseconds;

    [Benchmark]
    [Arguments(100, 10)]
    [Arguments(100, 20)]
    [Arguments(200, 10)]
    [Arguments(200, 20)]
    public void Benchmark(int a, int b)
    {
        if (AddExtra5Miliseconds)
            Thread.Sleep(a + b + 5);
        else
            Thread.Sleep(a + b);
    }
}
```

|    Method | AddExtra5Miliseconds |   a |  b |     Mean |     Error |    StdDev |
|---------- |--------------------- |---- |--- |---------:|----------:|----------:|
| Benchmark |                False | 100 | 10 | 110.1 ms | 0.0056 ms | 0.0044 ms |
| Benchmark |                False | 100 | 20 | 120.1 ms | 0.0155 ms | 0.0138 ms |
| Benchmark |                False | 200 | 10 | 210.2 ms | 0.0187 ms | 0.0175 ms |
| Benchmark |                False | 200 | 20 | 220.3 ms | 0.1055 ms | 0.0986 ms |
| Benchmark |                 True | 100 | 10 | 115.3 ms | 0.1375 ms | 0.1286 ms |
| Benchmark |                 True | 100 | 20 | 125.3 ms | 0.1212 ms | 0.1134 ms |
| Benchmark |                 True | 200 | 10 | 215.4 ms | 0.0779 ms | 0.0691 ms |
| Benchmark |                 True | 200 | 20 | 225.4 ms | 0.0775 ms | 0.0725 ms | 

## \[ArgumentsSource\]

In case you want to use a lot of values, you should use `[ArgumentsSource]`.

You can mark one or several fields or properties in your class by the `ArgumentsSource` attribute. In this attribute, you have to specify the name of public method/property which is going to provide the values (something that implements `IEnumerable`). The source must be within benchmarked type! 

```cs
public class IntroArgumentsSource
{
    [Benchmark]
    [ArgumentsSource(nameof(Numbers))]
    public double Pow(double x, double y) => Math.Pow(x, y);

    public IEnumerable<object[]> Numbers()
    {
        yield return new object[] { 1.0, 1.0 };
        yield return new object[] { 2.0, 2.0 };
        yield return new object[] { 4.0, 4.0 };
        yield return new object[] { 10.0, 10.0 };
    }
}
```

| Method |  x |  y |      Mean |     Error |    StdDev |
|------- |--- |--- |----------:|----------:|----------:|
|    Pow |  1 |  1 |  9.360 ns | 0.0190 ns | 0.0149 ns |
|    Pow |  2 |  2 | 40.624 ns | 0.3413 ns | 0.3192 ns |
|    Pow |  4 |  4 | 40.537 ns | 0.0560 ns | 0.0524 ns |
|    Pow | 10 | 10 | 40.395 ns | 0.3274 ns | 0.3063 ns |

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

| Method | someClass | someStruct |     Mean | Error |
|------- |---------- |----------- |---------:|------:|
| Simple |  10 items |         10 | 887.2 us |    NA |
| Simple |  15 items |         15 | 963.1 us |    NA |


## Allocation cost

The cost of creating the arguments is not included in the benchmark. So if you want to pass an array as an argument, we are going to allocate it before running the benchmark, and the benchmark will not include this operation.

```cs
[MemoryDiagnoser]
public class IntroArrayParam
{
    [Benchmark]
    [ArgumentsSource(nameof(Data))]
    public int ArrayIndexOf(int[] array, int value) => Array.IndexOf(array, value);

    [Benchmark]
    [ArgumentsSource(nameof(Data))]
    public int ManualIndexOf(int[] array, int value)
    {
        for (int i = 0; i < array.Length; i++)
            if (array[i] == value)
                return i;

        return -1;
    }

    public IEnumerable<object[]> Data()
    {
        yield return new object[] { ArrayParam<int>.ForPrimitives(new[] { 1, 2, 3 }), 4 };
        yield return new object[] { ArrayParam<int>.ForPrimitives(Enumerable.Range(0, 100).ToArray()), 4 };
        yield return new object[] { ArrayParam<int>.ForPrimitives(Enumerable.Range(0, 100).ToArray()), 101 };
    }
}
```

|        Method |      array | value |      Mean |     Error |    StdDev | Allocated |
|-------------- |----------- |------ |----------:|----------:|----------:|----------:|
|  **ArrayIndexOf** | **Array[100]** |     **4** | **15.558 ns** | **0.0638 ns** | **0.0597 ns** |       **0 B** |
| ManualIndexOf | Array[100] |     4 |  5.345 ns | 0.0668 ns | 0.0625 ns |       0 B |
|  **ArrayIndexOf** |   **Array[3]** |     **4** | **14.334 ns** | **0.1758 ns** | **0.1558 ns** |       **0 B** |
| ManualIndexOf |   Array[3] |     4 |  2.758 ns | 0.0905 ns | 0.1208 ns |       0 B |
|  **ArrayIndexOf** | **Array[100]** |   **101** | **78.359 ns** | **1.8853 ns** | **2.0955 ns** |       **0 B** |
| ManualIndexOf | Array[100] |   101 | 80.421 ns | 0.6391 ns | 0.5978 ns |       0 B |
