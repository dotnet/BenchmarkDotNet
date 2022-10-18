//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace MS.WindowsAPICodePack.Internal
{
    /// <summary>Common Helper methods</summary>
    public static class CoreHelpers
    {
        /// <summary>Determines if the application is running on Windows 7 or later</summary>
        public static bool RunningOnWin7OrLater =>
                // Verifies that OS version is 6.1 or greater, and the Platform is WinNT.
                Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Environment.OSVersion.Version.CompareTo(new Version(6, 1)) >= 0;

        /// <summary>Throws PlatformNotSupportedException if the application is not running on Windows 7</summary>
        public static void ThrowIfNotWin7OrLater()
        {
            if (!RunningOnWin7OrLater)
            {
                throw new PlatformNotSupportedException("Platform is not Windows 7 or later");
            }
        }
    }
}