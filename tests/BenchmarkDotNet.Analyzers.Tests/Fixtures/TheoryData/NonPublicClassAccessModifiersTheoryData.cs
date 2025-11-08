using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

internal sealed class NonPublicClassAccessModifiersTheoryData : TheoryData<string>
{
    public NonPublicClassAccessModifiersTheoryData()
    {
        AddRange(
            "protected internal ",
            "protected ",
            "internal ",
            "private protected ",
            "private ",
            "file ",
            ""
        );
    }
}