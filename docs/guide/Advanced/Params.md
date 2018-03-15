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

You can mark one or several fields or properties in your class by the `ParamsSource` attribute. In this attribute, you have to specify the name of public method/property which is going to provide the values (something that implements `IEnumerable`). The source must be within benchmarked type! If the values are complex types (not compile-time constants) you should implement `IParam` interface (see below).

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

 In case you want to use values which are not compile-time constants, you have to create a type which implements `IParam` interface. This is required because internally BenchmarkDotNet generates and compiles code for every benchmark. We know how to deal with primitive types, but we don't want to implement complex logic for creating complex types. This responsibility is transferred to the users ;)

## Example (IParam)

```cs
public class IntroIParam
{
    public struct VeryCustomStruct
    {
        public readonly int X, Y;

        public VeryCustomStruct(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class CustomParam : IParam
    {
        private readonly VeryCustomStruct value;

        public CustomParam(VeryCustomStruct value) => this.value = value;

        public object Value => value;

        public string DisplayText => $"({value.X},{value.Y})";

        public string ToSourceCode() => $"new VeryCustomStruct({value.X}, {value.Y})";
    }

    [ParamsSource(nameof(Parameters))]
    public VeryCustomStruct Field;

    public IEnumerable<IParam> Parameters()
    {
        yield return new CustomParam(new VeryCustomStruct(100, 10));
        yield return new CustomParam(new VeryCustomStruct(100, 20));
        yield return new CustomParam(new VeryCustomStruct(200, 10));
        yield return new CustomParam(new VeryCustomStruct(200, 20));
    }

    [Benchmark]
    public void Benchmark() => Thread.Sleep(Field.X + Field.Y);
}
```

|    Method |    Field |     Mean |     Error |    StdDev |
|---------- |--------- |---------:|----------:|----------:|
| Benchmark | (100,10) | 110.4 ms | 0.1148 ms | 0.1074 ms |
| Benchmark | (100,20) | 120.4 ms | 0.0843 ms | 0.0788 ms |
| Benchmark | (200,10) | 210.4 ms | 0.0892 ms | 0.0834 ms |
| Benchmark | (200,20) | 220.4 ms | 0.0949 ms | 0.0887 ms |
