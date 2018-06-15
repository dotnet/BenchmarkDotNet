using System;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(char))]
    public class IntroGenericTypeArguments<T>
    {
        [Benchmark] public T Create() => Activator.CreateInstance<T>();
    }
}