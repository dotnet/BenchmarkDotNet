using BenchmarkDotNet.Attributes;
using System;

namespace BenchmarkDotNet.Samples
{
    [ExceptionDiagnoser]
    public class IntroExceptionDiagnoser
    {
        [Benchmark] public void NoThrow() { }
        [Benchmark] public void Throw()
        {
            try { throw new Exception(); } catch { }
        }
    }
}
