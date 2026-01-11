using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Loggers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Helpers;

#nullable enable

internal static class ConsoleHelper
{
    private const string ESC = "\e";          // Escape sequence.
    private const string OSC8 = $"{ESC}]8;;"; // Operating System Command 8
    private const string ST = ESC + @"\";     // String Terminator

    /// <summary>
    /// Write clickable link to console.
    /// If console doesn't support OSC 8 hyperlinks. It writes plain link with markdown syntax.
    /// </summary>
    public static void WriteLineAsClickableLink(ILogger consoleLogger, string link, string? linkCaption = null, LogKind logKind = LogKind.Info, string prefixText = "", string suffixText = "")
    {
        if (prefixText != "")
            consoleLogger.Write(logKind, prefixText);

        WriteAsClickableLink(consoleLogger, link, linkCaption, logKind);

        // On Windows Terminal environment.
        // It need to write extra space to avoid link style corrupted issue that occurred when window resized.
        if (IsWindowsTerminal.Value && IsClickableLinkSupported.Value && suffixText == "")
            suffixText = " ";

        if (suffixText != "")
            consoleLogger.Write(logKind, suffixText);

        consoleLogger.WriteLine();
    }

    /// <summary>
    /// Write clickable link to console.
    /// If console doesn't support OSC 8 hyperlinks. It writes plain link with markdown syntax.
    /// </summary>
    public static void WriteAsClickableLink(ILogger consoleLogger, string link, string? linkCaption = null, LogKind logKind = LogKind.Info)
    {
        if (consoleLogger.Id != nameof(ConsoleLogger))
            throw new NotSupportedException("This method is expected logger that has ConsoleLogger id.");

        // If clickable link supported. Write clickable link with OSC8.
        if (IsClickableLinkSupported.Value)
        {
            consoleLogger.Write(logKind, @$"{OSC8}{link}{ST}{linkCaption ?? link}{OSC8}{ST}");
            return;
        }

        // If link caption is specified. Write link as plain text with markdown link syntax.
        if (!string.IsNullOrEmpty(linkCaption))
        {
            consoleLogger.Write(logKind, $"[{linkCaption}]({link})");
            return;
        }

        // Write link as plain text.
        consoleLogger.Write(logKind, link);
    }

    private static readonly Lazy<bool> IsWindowsTerminal = new(()
        => Environment.GetEnvironmentVariable("WT_SESSION") != null);

    private static readonly Lazy<bool> IsClickableLinkSupported = new(() =>
    {
        if (Console.IsOutputRedirected)
            return false;

        // The current console doesn't have a valid buffer size, which means it is not a real console.
        if (Console.BufferHeight == 0 || Console.BufferWidth == 0)
            return false;

        // Disable clickable link on CI environment.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            return false;

        // dumb terminal don't support ANSI escape sequence.
        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        if (term == "dumb")
            return false;

        if (OsDetector.IsWindows())
        {
            try
            {
                // conhost.exe don't support clickable link with OSC8.
                if (IsRunningOnConhost())
                    return false;

                // ConEmu and don't support OSC8.
                var conEmu = Environment.GetEnvironmentVariable("ConEmuANSI");
                if (conEmu != null)
                    return false;

                // Return true if Virtual Terminal Processing mode is enabled.
                return IsVirtualTerminalProcessingEnabled();
            }
            catch
            {
                return false; // Ignore unexpected exception.
            }
        }
        else
        {
            // screen don't support OSC8 clickable link.
            if (Regex.IsMatch(term, "^screen"))
                return false;

            // Other major terminal supports OSC8 by default. https://github.com/Alhadis/OSC8-Adoption
            return true;
        }
    });

    [SupportedOSPlatform("windows")]
    private static bool IsVirtualTerminalProcessingEnabled()
    {
        // Try to get Virtual Terminal Processing enebled or not.
        const uint STD_OUTPUT_HANDLE = unchecked((uint)-11);
        IntPtr handle = NativeMethods.GetStdHandle(STD_OUTPUT_HANDLE);
        if (handle == IntPtr.Zero)
            return false;

        if (NativeMethods.GetConsoleMode(handle, out uint consoleMode))
        {
            const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
            if ((consoleMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) > 0)
            {
                return true;
            }
        }
        return false;
    }

    [SupportedOSPlatform("windows")]
    private static bool IsRunningOnConhost()
    {
        IntPtr hwnd = NativeMethods.GetConsoleWindow();
        if (hwnd == IntPtr.Zero)
            return false;

        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        using var process = Process.GetProcessById((int)pid);
        return process.ProcessName == "conhost";
    }

    [SupportedOSPlatform("windows")]
    private static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(uint nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
