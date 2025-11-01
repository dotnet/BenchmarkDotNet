namespace BenchmarkDotNet.Analyzers.Tests.Fixtures
{
    using Xunit;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public static class TheoryDataExtensions
    {
        public static ReadOnlyCollection<T> AsReadOnly<T>(this TheoryData<T> theoryData) => (theoryData as IEnumerable<T>).ToList()
                                                                                                                          .AsReadOnly();
    }
}