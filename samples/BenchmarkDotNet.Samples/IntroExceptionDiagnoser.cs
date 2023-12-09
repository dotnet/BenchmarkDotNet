using BenchmarkDotNet.Attributes;
using System;

namespace BenchmarkDotNet.Samples
{
    [ExceptionDiagnoser]
    public class IntroExceptionDiagnoser
    {
        [Benchmark]
        public void ThrowExceptionRandomly()
        {
            try
            {
                if (new Random().Next(0, 5) > 1)
                {
                    throw new Exception();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
