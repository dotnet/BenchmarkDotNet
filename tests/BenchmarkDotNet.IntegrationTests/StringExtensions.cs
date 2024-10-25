using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.IntegrationTests;

internal static class StringExtensions
{
    private static readonly char[] newLine = Environment.NewLine.ToCharArray();

    /// <summary>
    /// Creates an array of strings by splitting this string at each occurrence of a newLine separator.
    /// Contrary to calling <code>value.Split('\r', '\n')</code>, this method does not return an empty
    /// string when CR is followed by LF.
    /// </summary>
    public static IEnumerable<string> EnumerateLines(this string value)
    {
        int pos;
        while ((pos = value.IndexOfAny(newLine)) >= 0)
        {
            yield return value.Substring(0, pos);
            int stride = value[pos] == '\r' && value[pos + 1] == '\n' ? 2 : 1;
            value = value.Substring(pos + stride);
        }
        if (value.Length > 0)
        {
            yield return value;
        }
    }
}
