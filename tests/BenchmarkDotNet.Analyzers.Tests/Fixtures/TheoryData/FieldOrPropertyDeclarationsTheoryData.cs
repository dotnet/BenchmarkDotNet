using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures
{
    internal sealed class FieldOrPropertyDeclarationsTheoryData : TheoryData<string>
    {
        public FieldOrPropertyDeclarationsTheoryData()
        {
            AddRange(
#if NET5_0_OR_GREATER
                     "Property { get; init; }",
#else
                     "Property { get; set; }",
#endif
                     "_field;"
                    );
        }
    }
}
