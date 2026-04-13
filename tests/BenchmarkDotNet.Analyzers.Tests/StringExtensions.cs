#if NETFRAMEWORK
namespace BenchmarkDotNet.Analyzers.Tests;

internal static class StringExtensions
{
    public static string ReplaceLineEndings(this string input)
        => ReplaceLineEndings(input, Environment.NewLine);

    private static string ReplaceLineEndings(this string text, string replacementText)
    {
        text = text.Replace("\r\n", "\n");

        if (replacementText != "\n")
            text = text.Replace("\n", replacementText);

        return text;
    }
}
#endif
