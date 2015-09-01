using BenchmarkDotNet.Tasks;
using System;
using System.Threading;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupAttributeInvalidMethodTest : IntegrationTestBase
    {
        [Fact]
        public void Test()
        {
            Assert.Throws<InvalidOperationException>(() => new BenchmarkRunner().RunCompetition(new SetupAttributeInvalidMethodTest()));
        }

        [Setup]
        public void Setup(int someParameters) // [Setup] methods must have no parameters
        {
            Console.WriteLine("// ### Setup called ###");
        }

        [Benchmark]
        [BenchmarkTask(processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Benchmark()
        {
            Thread.Sleep(5);
        }
    }
}
