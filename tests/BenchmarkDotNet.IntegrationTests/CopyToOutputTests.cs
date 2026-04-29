#if NETFRAMEWORK
using BenchmarkDotNet.IntegrationTests.CustomPaths;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CopyToOutputTests : BenchmarkTestExecutor
    {
        public CopyToOutputTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CopyToOutputIsSupported() => CanExecute<BenchmarksThatUsesFileFromOutput>();
    }
}
#endif