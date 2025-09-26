using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures
{
    internal sealed class FieldOrPropertyDeclarationTheoryData : TheoryData<string>
    {
        public FieldOrPropertyDeclarationTheoryData()
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
