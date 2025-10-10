namespace BenchmarkDotNet.Analyzers.Tests.Fixtures
{
    using Xunit;

    internal sealed class NonPublicClassMemberAccessModifiersTheoryData : TheoryData<string>
    {
        public NonPublicClassMemberAccessModifiersTheoryData()
        {
            AddRange(
                     "protected internal ",
                     "protected ",
                     "internal ",
                     "private protected ",
                     "private ",
                     ""
                    );
        }
    }
}
