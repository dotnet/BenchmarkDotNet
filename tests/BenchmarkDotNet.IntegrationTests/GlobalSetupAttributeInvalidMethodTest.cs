using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
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
            var summary = CanExecute<GlobalSetupAttributeInvalidMethod>(fullValidation: false);
            Assert.Equal("GlobalSetup method GlobalSetup has incorrect signature.\nMethod shouldn't have any arguments.", summary.Title);
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
