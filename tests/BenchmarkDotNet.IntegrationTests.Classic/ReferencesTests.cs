using BenchmarkDotNet.IntegrationTests.CustomPaths;
using BenchmarkDotNet.IntegrationTests.DifferentRuntime;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class ReferencesTests
    {
        private readonly ITestOutputHelper output;
        public ReferencesTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void FSharpIsSupported() => Run<FSharpBenchmark.Db>();

        [Fact]
        public void VisualBasicIsSupported() => Run<VisualBasic.Sample>();

        [Fact]
        public void BenchmarksThatUseTypeFromCustomPathDllAreSupported() => Run<BenchmarksThatUseTypeFromCustomPathDll>();

        [Fact]
        public void BenchmarksThatReturnTypeFromCustomPathDllAreSupported() => Run<BenchmarksThatReturnTypeFromCustomPathDll>();

        [Fact]
        public void BenchmarksThatReturnTypeThatRequiresDifferentRuntimeAreSupported() => Run<BenchmarksThatReturnTypeThatRequiresDifferentRuntime>();

        [Fact]
        public void BenchmarksThatUseTypeThatRequiresDifferentRuntimeAreSupported() => Run<BenchmarksThatUseTypeThatRequiresDifferentRuntime>();

        private void Run<T>() => BenchmarkTestRunner.CanCompileAndRun<T>(output);
    }
}