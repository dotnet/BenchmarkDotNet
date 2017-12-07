#if CLASSIC
using BenchmarkDotNet.IntegrationTests.CustomPaths;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ReferencesTests : BenchmarkTestExecutor
    {
        public ReferencesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BenchmarksThatUseTypeFromCustomPathDllAreSupported() 
            => CanExecute<BenchmarksThatUseTypeFromCustomPathDll>();

        [Fact]
        public void BenchmarksThatReturnTypeFromCustomPathDllAreSupported() 
            => CanExecute<BenchmarksThatReturnTypeFromCustomPathDll>();

        [Fact]
        public void FSharpIsSupported() => CanExecute<FSharpBenchmark.Db>();

        [Fact]
        public void VisualBasicIsSupported() => CanExecute<VisualBasic.Sample>();
    }
}
#endif