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

        /*
         * as of 2nd of April 2017 VS 2017 can't handle it yet, as soon as this starts working we should uncomment it
         * https://github.com/dotnet/project-system/pull/1670#issuecomment-289902007
        [Fact]
        public void FSharpIsSupported() => CanExecute<FSharpBenchmark.Db>();

        [Fact]
        public void VisualBasicIsSupported() => CanExecute<VisualBasic.Sample>();
        */
    }
}
#endif