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
    /// Try to gets clickable link text for console.
    /// If console doesn't support clickable link, it returns false.
    /// </summary>
    public static bool TryGetClickableLink(string link, string? linkCaption, out string result)
    {
        if (!IsClickableLinkSupported)
        {
            result = "";
            return false;
        }

        result = @$"{OSC8}{link}{ST}{linkCaption ?? link}{OSC8}{ST}";
        return true;
    }

    public static bool IsWindowsTerminal => _isWindowsTerminal.Value;

    public static bool IsClickableLinkSupported => _isClickableLinkSupported.Value;

    private static readonly Lazy<bool> _isWindowsTerminal = new(()
        => Environment.GetEnvironmentVariable("WT_SESSION") != null);

    private static readonly Lazy<bool> _isClickableLinkSupported = new(() =>
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
