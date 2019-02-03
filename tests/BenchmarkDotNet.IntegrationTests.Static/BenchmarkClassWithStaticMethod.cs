using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.Static
{
    [DryJob]
    public class BenchmarkClassWithStaticMethodsOnly
    {
        [Benchmark]
        public static void StaticMethod() { }
    }

    [DryJob]
    public class BenchmarkClassWithInstanceMethod
    {
        [Benchmark]
        public void NonStaticMethod() { }
    }
}
