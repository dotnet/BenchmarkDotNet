using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupAttributeInvalidMethodTest
    {
        [Fact]
        public void Test()
        {
            Assert.Throws<InvalidOperationException>(() => BenchmarkTestExecutor.CanExecute<SetupAttributeInvalidMethodTest>());
        }

        [Setup]
        public void Setup(int someParameters) // [Setup] methods must have no parameters
        {
            Console.WriteLine("// ### Setup called ###");
        }

        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(5);
        }
    }
}
