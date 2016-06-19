using BenchmarkDotNet.IntegrationTests.CustomPaths;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class CopyToOutputTests
    {
        [Fact]
        public void CopyToOutputIsSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatUsesFileFromOutput>();
        }
    }
}