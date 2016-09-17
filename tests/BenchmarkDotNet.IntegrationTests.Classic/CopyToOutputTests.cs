using BenchmarkDotNet.IntegrationTests.CustomPaths;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class CopyToOutputTests
    {
        private readonly ITestOutputHelper output;

        public CopyToOutputTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CopyToOutputIsSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatUsesFileFromOutput>(output);
        }
    }
}