using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers;

internal static class Assertion
{
    [AssertionMethod]
    public static void NotNull(string name, object? value)
    {
        if (value == null)
            throw new ArgumentNullException(name, $"{name} can't be null");
    }
}