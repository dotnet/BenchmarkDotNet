# Params

You can mark one or several fields or properties in your class by the `Params` attribute. In this attribute, you can specify set of values. Every value must be a compile-time constant.
As a result, you will get results for each combination of params values.

## Example (Params)

```cs
public class IntroParams
{
    [Params(100, 200)]
    public int A { get; set; }

    [Params(10, 20)]
    public int B { get; set; }

    [Benchmark]
    public void Benchmark()
    {
        Thread.Sleep(A + B + 5);
    }
}
```

   Method  |      Median |    StdDev |   A |  B
---------- |------------ |---------- |---- |---
 Benchmark | 115.3325 ms | 0.0242 ms | 100 | 10
 Benchmark | 125.3282 ms | 0.0245 ms | 100 | 20
 Benchmark | 215.3024 ms | 0.0375 ms | 200 | 10
 Benchmark | 225.2710 ms | 0.0434 ms | 200 | 20

# ParamsSource

In case you want to use a lot of values, you should use `[ParamsSource]`.

You can mark one or several fields or properties in your class by the `ParamsSource` attribute. In this attribute, you have to specify the name of public method/property which is going to provide the values (something that implements `IEnumerable`). The source must be within benchmarked type!

## Example (ParamsSource)

```cs
public class IntroParamsSource
{
    [ParamsSource(nameof(ValuesForA))]
    public int A { get; set; } // property with public setter

    [ParamsSource(nameof(ValuesForB))]
    public int B; // public field

    public IEnumerable<int> ValuesForA => new[] { 100, 200 }; // public property

    public static IEnumerable<int> ValuesForB() => new[] { 10, 20 }; // public static method

    [Benchmark]
    public void Benchmark() => Thread.Sleep(A + B + 5);
}
```

   Method  |      Median |    StdDev |   A |  B
---------- |------------ |---------- |---- |---
 Benchmark | 115.3325 ms | 0.0242 ms | 100 | 10
 Benchmark | 125.3282 ms | 0.0245 ms | 100 | 20
 Benchmark | 215.3024 ms | 0.0375 ms | 200 | 10
 Benchmark | 225.2710 ms | 0.0434 ms | 200 | 20


# IParam

 You don't need to use `IParam` anymore since `0.11.0`. Just use complex types as you wish and override `ToString` method to change the display names used in the results.