using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples;

public class IntroSmokeIncrements
{
    public int Field;

    [Benchmark]
    public void Increment01()
    {
        Field++;
    }

    [Benchmark]
    public void Increment02()
    {
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment03()
    {
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment04()
    {
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment05()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment06()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment07()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment08()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment09()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment10()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }

    [Benchmark]
    public void Increment20()
    {
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
        Field++;
    }
}