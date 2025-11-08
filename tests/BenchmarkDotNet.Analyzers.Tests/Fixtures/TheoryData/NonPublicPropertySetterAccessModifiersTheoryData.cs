using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

internal sealed class NonPublicPropertySetterAccessModifiersTheoryData : TheoryData<string>
{
    public NonPublicPropertySetterAccessModifiersTheoryData()
    {
        AddRange(
            "protected internal",
            "protected",
            "internal",
            "private protected",
            "private"
        );
    }
}