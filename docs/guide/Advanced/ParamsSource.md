# ParamsSource

You can mark one or several fields or properties in your class by the `ParamsSource` attribute. In this attribute, you have to specify the name of public method/property which is going to provide the values. The source must be within benchmarked type!

## Example

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
