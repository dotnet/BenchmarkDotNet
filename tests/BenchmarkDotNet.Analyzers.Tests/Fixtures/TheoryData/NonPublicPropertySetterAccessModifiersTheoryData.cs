namespace BenchmarkDotNet.Analyzers.Tests.Fixtures
{
    using Xunit;

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
}
