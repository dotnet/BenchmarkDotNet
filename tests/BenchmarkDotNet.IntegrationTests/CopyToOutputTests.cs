#if NETFRAMEWORK
using BenchmarkDotNet.IntegrationTests.CustomPaths;
using Xunit;
using Xunit.Abstractions;

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