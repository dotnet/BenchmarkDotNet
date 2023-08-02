using System;
using System.IO;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    /// <summary>
    /// Updates Console.Title, subject to platform capabilities and Console availability.
    /// Restores the original (or fallback) title upon disposal.
    /// </summary>
    internal class ConsoleTitler : IDisposable
    {
        /// <summary>
        /// Whether this instance has any effect. This will be false if the platform doesn't support Console retitling,
        /// or if Console output is redirected.
        /// </summary>
        public bool IsEnabled { get; private set; }

        private string oldConsoleTitle;

        public ConsoleTitler(string initialTitle)
        {
            try
            {
                // Return without enabling if Console output is redirected.
                if (Console.IsOutputRedirected)
                {
                    return;
                }
            }
            catch (PlatformNotSupportedException)
            {
                // Ignore the exception. Some platforms do not support Console.IsOutputRedirected.
            }

            try
            {
                oldConsoleTitle = PlatformSupportsTitleRead() ? Console.Title : "";
            }
            catch (IOException)
            {
                // We're unable to read Console.Title on a platform that supports it. This can happen when no console
                // window is available due to the application being Windows Forms, WPF, Windows Service or a daemon.
                oldConsoleTitle = "";
            }

            try
            {
                // Enable ConsoleTitler if and only if we can successfully set the Console.Title property.
                Console.Title = initialTitle;
                IsEnabled = true;
            }
            catch (IOException)
            {
            }
            catch (PlatformNotSupportedException)
            {
                // As of .NET 7, platforms other than Windows, Linux and MacOS do not support Console retitling.
            }
        }

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatformGuard("windows")]
#endif
        private static bool PlatformSupportsTitleRead() => RuntimeInformation.IsWindows();

        /// <summary>
        /// Updates Console.Title if enabled.
        /// </summary>
        public void UpdateTitle(string title)
        {
            if (IsEnabled)
            {
                Console.Title = title;
            }
        }

        public void Dispose()
        {
            if (IsEnabled)
            {
                Console.Title = oldConsoleTitle;
                IsEnabled = false;
            }
        }
    }
}
