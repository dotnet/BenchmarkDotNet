using System.Collections.ObjectModel;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public static class TheoryDataExtensions
{
    public static ReadOnlyCollection<T> AsReadOnly<T>(this TheoryData<T> theoryData)
        => (theoryData as IEnumerable<T>).ToList().AsReadOnly();
}