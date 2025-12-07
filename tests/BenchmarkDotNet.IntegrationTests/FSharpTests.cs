using Xunit;
using Xunit.Abstractions;
using static FSharpBenchmarks;

namespace BenchmarkDotNet.IntegrationTests
{
    public class FSharpTests : BenchmarkTestExecutor
    {
        public FSharpTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ParamsSupportFSharpEnums() => CanExecute<EnumParamsTest>();

        // #2530
        [Fact]
        public void FSharpAnonymousRecordIsSupported() => CanExecute<AnonymousRecordTest>();
    }
}
