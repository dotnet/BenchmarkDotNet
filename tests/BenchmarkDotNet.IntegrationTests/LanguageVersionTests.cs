using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class LanguageVersionTests : BenchmarkTestExecutor
    {
        public LanguageVersionTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void WeSupportCsharp72() => CanExecute<UsingCsharp7_2>();
    }

    public class UsingCsharp7_2
    {
        private protected int fieldWithCsharp7_2_access = 7;

        [Benchmark]
        public ref int Benchmark() => ref fieldWithCsharp7_2_access;
    }
}
