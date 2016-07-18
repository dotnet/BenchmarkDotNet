using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupAttributeInvalidMethodTest : BenchmarkTestExecutor
    {
        public SetupAttributeInvalidMethodTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SetupAttributeMethodsMustHaveNoParameters()
        {
            Assert.Throws<InvalidOperationException>(() => CanExecute<SetupAttributeInvalidMethod>());
        }

        public class SetupAttributeInvalidMethod
        {
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
}
