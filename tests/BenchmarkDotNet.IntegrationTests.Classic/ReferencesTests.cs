using BenchmarkDotNet.IntegrationTests.CustomPaths;
using BenchmarkDotNet.IntegrationTests.DifferentRuntime;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class ReferencesTests
    {
        private readonly ITestOutputHelper output;

        public ReferencesTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void FSharpIsSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<FSharpBenchmark.Db>(output);
        }

        [Fact]
        public void VisualBasicIsSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<VisualBasic.Sample>(output);
        }

        [Fact]
        public void BenchmarksThatUseTypeFromCustomPathDllAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatUseTypeFromCustomPathDll>(output);
        }

        [Fact]
        public void BenchmarksThatReturnTypeFromCustomPathDllAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatReturnTypeFromCustomPathDll>(output);
        }

        [Fact]
        public void BenchmarksThatReturnTypeThatRequiresDifferentRuntimeAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatReturnTypeThatRequiresDifferentRuntime>(output);
        }

        [Fact]
        public void BenchmarksThatUseTypeThatRequiresDifferentRuntimeAreSupported()
        {
            BenchmarkTestRunner.CanCompileAndRun<BenchmarksThatUseTypeThatRequiresDifferentRuntime>(output);
        }
    }
}
