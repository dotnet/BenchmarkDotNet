using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ReferencesTests : BenchmarkTestExecutor
    {
        public ReferencesTests(ITestOutputHelper output) : base(output) { }

#if NETFRAMEWORK
        [Fact]
        public void BenchmarksThatUseTypeFromCustomPathDllAreSupported()
            => CanExecute<BenchmarkDotNet.IntegrationTests.CustomPaths.BenchmarksThatUseTypeFromCustomPathDll>();

        [Fact]
        public void BenchmarksThatReturnTypeFromCustomPathDllAreSupported()
            => CanExecute<BenchmarkDotNet.IntegrationTests.CustomPaths.BenchmarksThatReturnTypeFromCustomPathDll>();
#endif
        [Fact]
        public void FSharpIsSupported() => CanExecute<FSharpBenchmarks.Db>();

        [Fact]
        public void VisualBasicIsSupported() => CanExecute<VisualBasic.Sample>();
    }
}
