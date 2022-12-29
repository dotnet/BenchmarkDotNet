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

        /// <summary>
        /// Constructs a ConsoleTitler
        /// </summary>
        /// <param name="fallbackTitle">What to restore Console.Title to upon disposal (for platforms with write-only Console.Title)</param>
        public ConsoleTitler(string fallbackTitle)
        {
            // Return without enabling if Console output is redirected, or if we're not on a platform that supports Console retitling.
            if (Console.IsOutputRedirected || !PlatformSupportsTitleWrite())
            {
                return;
            }

            try
            {
                oldConsoleTitle = PlatformSupportsTitleRead() ? Console.Title : fallbackTitle;
                IsEnabled = true;
            }
            catch (IOException)
            {
                // We're unable to read Console.Title on a platform that supports it. This can happen when no console
                // window is available due to the application being Windows Forms, WPF, Windows Service or a daemon.
                // Because we won't be able to write Console.Title either, return without enabling the titler.
            }
        }

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatformGuard("windows")]
#endif
        private static bool PlatformSupportsTitleRead() => RuntimeInformation.IsWindows();

        private static bool PlatformSupportsTitleWrite() =>
            RuntimeInformation.IsWindows() ||
            RuntimeInformation.IsLinux() ||
            RuntimeInformation.IsMacOS();

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

        /// <summary>
        /// Updates Console.Title if enabled, using a Func to avoid potential string-building cost.
        /// </summary>
        public void UpdateTitle(Func<string> title)
        {
            if (IsEnabled)
            {
                Console.Title = title();
            }
        }

        public void Dispose()
        {
            if (IsEnabled && oldConsoleTitle != null)
            {
                Console.Title = oldConsoleTitle;
                IsEnabled = false;
            }
        }
    }
}
