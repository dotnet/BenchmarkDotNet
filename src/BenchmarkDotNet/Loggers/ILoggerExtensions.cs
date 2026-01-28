using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Loggers;

#nullable enable

public static class ILoggerExtensions
{
    /// <summary>
    /// Write clickable link to logger.
    /// If the logger doesn't implement <see cref="ILinkLogger"/>. It's written as plain text.
    /// </summary>
    public static void WriteLink(this ILogger logger, string link, string? linkCaption = null, LogKind logKind = LogKind.Info)
    {
        if (logger is ILinkLogger)
        {
            if (ConsoleHelper.TryGetClickableLink(link, linkCaption, out var clickableLink))
                link = clickableLink;
        }

        logger.Write(logKind, link);
    }

    /// <summary>
    /// Write clickable link to logger.
    /// If the logger doesn't implement <see cref="ILinkLogger"/>. It's written as plain text.
    /// </summary>
    public static void WriteLineLink(this ILogger logger, string link, string? linkCaption = null, string prefixText = "", string suffixText = "", LogKind logKind = LogKind.Info)
    {
        if (logger is ILinkLogger)
        {
            if (ConsoleHelper.TryGetClickableLink(link, linkCaption, out var clickableLink))
            {
                link = clickableLink;

                // Temporary workaround for Windows Terminal.
                // To avoid link style corruption issue when output ends with a clickable link and window is resized.
                if (ConsoleHelper.IsWindowsTerminal && suffixText == "")
                    suffixText = " ";
            }
        }

        logger.WriteLine(logKind, $"{prefixText}{link}{suffixText}");
    }
}
