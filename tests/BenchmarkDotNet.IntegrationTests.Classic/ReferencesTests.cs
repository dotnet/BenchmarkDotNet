using BenchmarkDotNet.IntegrationTests.CustomPaths;
using BenchmarkDotNet.IntegrationTests.DifferentRuntime;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class ReferencesTests
    {
        [Fact]
        public void FSharpIsSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<FSharpBenchmark.Db>();
        }

        [Fact]
        public void VisualBasicIsSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<VisualBasic.Sample>();
        }

        [Fact]
        public void BenchmarksThatUseTypeFromCustomPathDllAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatUseTypeFromCustomPathDll>();
        }

        [Fact]
        public void BenchmarksThatReturnTypeFromCustomPathDllAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatReturnTypeFromCustomPathDll>();
        }

        [Fact]
        public void BenchmarksThatReturnTypeThatRequiresDifferentRuntimeAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatReturnTypeThatRequiresDifferentRuntime>();
        }

        [Fact]
        public void BenchmarksThatUseTypeThatRequiresDifferentRuntimeAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatUseTypeThatRequiresDifferentRuntime>();
        }
    }
}
