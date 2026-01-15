using BenchmarkDotNet.Helpers;
using System.Text;

namespace BenchmarkDotNet.Loggers;

/// <summary>
/// Marker interface for logger that support clickable link with ANSI escape sequence.
/// </summary>
internal interface IConsoleLogger : ILogger
{
}

/// <summary>
/// ExtensionMethods for IConsoleLogger interface.
/// </summary>
internal static class IConsoleLoggerExtensions
{
    /// <summary>
    /// Write clickable link to console.
    /// If console doesn't support clickable link. It writes plain link.
    /// </summary>
    public static void WriteLineLink(this IConsoleLogger logger, string link, string? linkCaption = null, LogKind logKind = LogKind.Info, string prefixText = "", string suffixText = "")
    {
        var sb = new StringBuilder();

        if (prefixText != "")
            sb.Append(prefixText);

        if (ConsoleHelper.TryGetClickableLink(link, linkCaption, out var linkText))
        {
            sb.Append(linkText);

            // Temporary workaround for Windows Terminal.
            // To avoid link style corruption issue when output ends with a clickable link and window is resized.
            if (ConsoleHelper.IsWindowsTerminal && suffixText == "")
                sb.Append(' ');
        }
        else
        {
            sb.Append(link); // Write plain link.
        }

        if (suffixText != "")
            sb.Append(suffixText);

        logger.WriteLine(logKind, sb.ToString());
    }
}
