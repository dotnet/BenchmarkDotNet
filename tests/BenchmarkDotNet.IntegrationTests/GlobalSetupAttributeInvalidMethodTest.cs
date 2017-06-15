using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GlobalSetupAttributeInvalidMethodTest : BenchmarkTestExecutor
    {
        public GlobalSetupAttributeInvalidMethodTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GlobalSetupAttributeMethodsMustHaveNoParameters()
        {
            Assert.Throws<InvalidOperationException>(() => CanExecute<GlobalSetupAttributeInvalidMethod>());
        }

        public class GlobalSetupAttributeInvalidMethod
        {
            [GlobalSetup]
            public void GlobalSetup(int someParameters) // [GlobalSetup] methods must have no parameters
            {
                Console.WriteLine("// ### GlobalSetup called ###");
            }

            [Benchmark]
            public void Benchmark()
            {
                Thread.Sleep(5);
            }
        }
    }
}
