namespace BenchmarkDotNet.Analyzers.Tests.Fixtures
{
    using Xunit;

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
}
