using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    // It is very easy to use BenchmarkDotNet. You should just create a class
    public class DoNothing
    {
        // And define a method with the Benchmark attribute
        [Benchmark]
        public void Sleep() { Thread.Sleep(1000); }

    }
}
