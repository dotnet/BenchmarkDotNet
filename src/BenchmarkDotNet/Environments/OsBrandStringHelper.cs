using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Environments
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class OsBrandStringHelper
    {
        // See https://en.wikipedia.org/wiki/Ver_(command)
        // See https://docs.microsoft.com/en-us/windows/release-health/release-information
        // See https://docs.microsoft.com/en-us/windows/release-health/windows11-release-information
        private static readonly Dictionary<string, string> WindowsBrandVersions = new Dictionary<string, string>
        {
            { "1.04", "1.0" },
            { "2.11", "2.0" },
            { "3", "3.0" },
            { "3.10.528", "NT 3.1" },
            { "3.11", "for Workgroups 3.11" },
            { "3.50.807", "NT 3.5" },
            { "3.51.1057", "NT 3.51" },
            { "4.00.950", "95" },
            { "4.00.1111", "95 OSR2" },
            { "4.03.1212-1214", "95 OSR2.1" },
            { "4.03.1214", "95 OSR2.5" },
            { "4.00.1381", "NT 4.0" },
            { "4.10.1998", "98" },
            { "4.10.2222", "98 SE" },
            { "4.90.2380.2", "ME Beta" },
            { "4.90.2419", "ME Beta 2" },
            { "4.90.3000", "ME" },
            { "5.00.1515", "NT 5.0 Beta" },
            { "5.00.2031", "2000 Beta 3" },
            { "5.00.2128", "2000 RC2" },
            { "5.00.2183", "2000 RC3" },
            { "5.00.2195", "2000" },
            { "5.0.2195", "2000 Professional" },
            { "5.1.2505", "XP RC1" },
            { "5.1.2600", "XP" },
            { "5.1.2600.1105-1106", "XP SP1" },
            { "5.1.2600.2180", "XP SP2" },
            { "5.2.3541", ".NET Server interim" },
            { "5.2.3590", ".NET Server Beta 3" },
            { "5.2.3660", ".NET Server RC1" },
            { "5.2.3718", ".NET Server 2003 RC2" },
            { "5.2.3763", "Server 2003 Beta" },
            { "5.2.3790", "XP Professional x64 Edition" },
            { "5.2.3790.1180", "Server 2003 SP1" },
            { "5.2.3790.1218", "Server 2003" },
            { "6.0.5048", "Longhorn" },
            { "6.0.5112", "Vista Beta 1" },
            { "6.0.5219", "Vista CTP" },
            { "6.0.5259", "Vista TAP Preview" },
            { "6.0.5270", "Vista CTP December" },
            { "6.0.5308", "Vista CTP February" },
            { "6.0.5342", "Vista CTP Refresh" },
            { "6.0.5365", "Vista April EWD" },
            { "6.0.5381", "Vista Beta 2 Preview" },
            { "6.0.5384", "Vista Beta 2" },
            { "6.0.5456", "Vista Pre-RC1 Build 5456" },
            { "6.0.5472", "Vista Pre-RC1 Build 5472" },
            { "6.0.5536", "Vista Pre-RC1 Build 5536" },
            { "6.0.5600.16384", "Vista RC1" },
            { "6.0.5700", "Vista Pre-RC2" },
            { "6.0.5728", "Vista Pre-RC2 Build 5728" },
            { "6.0.5744.16384", "Vista RC2" },
            { "6.0.5808", "Vista Pre-RTM Build 5808" },
            { "6.0.5824", "Vista Pre-RTM Build 5824" },
            { "6.0.5840", "Vista Pre-RTM Build 5840" },
            { "6.0.6000", "Vista" },
            { "6.0.6000.16386", "Vista RTM" },
            { "6.0.6001", "Vista SP1" },
            { "6.0.6002", "Vista SP2" },
            { "6.1.7600", "7" },
            { "6.1.7600.16385", "7" },
            { "6.1.7601", "7 SP1" },
            { "6.1.8400", "Home Server 2011" },
            { "6.2.8102", "8 Developer Preview" },
            { "6.2.9200", "8" },
            { "6.2.9200.16384", "8 RTM" },
            { "6.2.10211", "Phone 8" },
            { "6.3.9600", "8.1" },
            { "6.4.9841", "10 Technical Preview 1" },
            { "6.4.9860", "10 Technical Preview 2" },
            { "6.4.9879", "10 Technical Preview 3" },
            { "10.0.9926", "10 Technical Preview 4" },
            { "10.0.10041", "10 Technical Preview 5" },
            { "10.0.10049", "10 Technical Preview 6" },
            { "10.0.10240", "10 Threshold 1 [1507, RTM]" },
            { "10.0.10586", "10 Threshold 2 [1511, November Update]" },
            { "10.0.14393", "10 Redstone 1 [1607, Anniversary Update]" },
            { "10.0.15063", "10 Redstone 2 [1703, Creators Update]" },
            { "10.0.16299", "10 Redstone 3 [1709, Fall Creators Update]" },
            { "10.0.17134", "10 Redstone 4 [1803, April 2018 Update]" },
            { "10.0.17763", "10 Redstone 5 [1809, October 2018 Update]" },
            { "10.0.18362", "10 19H1 [1903, May 2019 Update]" },
            { "10.0.18363", "10 19H2 [1909, November 2019 Update]" },
            { "10.0.19041", "10 20H1 [2004, May 2020 Update]" },
            { "10.0.19042", "10 20H2 [20H2, October 2020 Update]" },
            { "10.0.19043", "10 21H1 [21H1, May 2021 Update]" },
            { "10.0.19044", "10 21H2 [21H2, November 2021 Update]" },
            { "10.0.19045", "10 22H2 [22H2, 2022 Update]" },
            { "10.0.22000", "11 21H2 [21H2, Sun Valley]" },
            { "10.0.22621", "11 22H2 [22H2, Sun Valley 2]" },
        };

        private class Windows1XVersion
        {
            private string? CodeVersion { get; }
            private string? CodeName { get; }
            private string? MarketingName { get; }
            private int BuildNumber { get; }

            private string MarketingNumber => BuildNumber >= 22000 ? "11" : "10";
            private string? ShortifiedCodeName => CodeName?.Replace(" ", "");
            private string? ShortifiedMarketingName => MarketingName?.Replace(" ", "");

            private Windows1XVersion(string? codeVersion, string? codeName, string? marketingName, int buildNumber)
            {
                CodeVersion = codeVersion;
                CodeName = codeName;
                MarketingName = marketingName;
                BuildNumber = buildNumber;
            }

            private string ToFullVersion(int? ubr = null)
                => ubr == null ? $"10.0.{BuildNumber}" : $"10.0.{BuildNumber}.{ubr}";

            private static string Collapse(params string[] values) => string.Join("/", values.Where(v => !string.IsNullOrEmpty(v)));

            // The line with OsBrandString is one of the longest lines in the summary.
            // When people past in on GitHub, it can be a reason of an ugly horizontal scrollbar.
            // To avoid this, we are trying to minimize this line and use the minimum possible number of characters.
            public string ToPrettifiedString(int? ubr)
                => CodeVersion == ShortifiedCodeName
                    ? $"{MarketingNumber} ({Collapse(ToFullVersion(ubr), CodeVersion, ShortifiedMarketingName)})"
                    : $"{MarketingNumber} ({Collapse(ToFullVersion(ubr), CodeVersion, ShortifiedMarketingName, ShortifiedCodeName)})";

            // See https://en.wikipedia.org/wiki/Windows_10_version_history
            // See https://en.wikipedia.org/wiki/Windows_11_version_history
            private static readonly List<Windows1XVersion> WellKnownVersions = new ()
            {
                // Windows 10
                new Windows1XVersion("1507", "Threshold 1", "RTM", 10240),
                new Windows1XVersion("1511", "Threshold 2", "November Update", 10586),
                new Windows1XVersion("1607", "Redstone 1", "Anniversary Update", 14393),
                new Windows1XVersion("1703", "Redstone 2", "Creators Update", 15063),
                new Windows1XVersion("1709", "Redstone 3", "Fall Creators Update", 16299),
                new Windows1XVersion("1803", "Redstone 4", "April 2018 Update", 17134),
                new Windows1XVersion("1809", "Redstone 5", "October 2018 Update", 17763),
                new Windows1XVersion("1903", "19H1", "May 2019 Update", 18362),
                new Windows1XVersion("1909", "19H2", "November 2019 Update", 18363),
                new Windows1XVersion("2004", "20H1", "May 2020 Update", 19041),
                new Windows1XVersion("20H2", "20H2", "October 2020 Update", 19042),
                new Windows1XVersion("21H1", "21H1", "May 2021 Update", 19043),
                new Windows1XVersion("21H2", "21H2", "November 2021 Update", 19044),
                new Windows1XVersion("22H2", "22H2", "2022 Update", 19045),
                // Windows 11
                new Windows1XVersion("21H2", "Sun Valley", null, 22000),
                new Windows1XVersion("22H2", "Sun Valley 2", "2022 Update", 22621),
                new Windows1XVersion("23H2", "Sun Valley 3", "2023 Update", 22631),
            };

            public static Windows1XVersion? Resolve(string osVersionString)
            {
                var windows1XVersion = WellKnownVersions.FirstOrDefault(v => osVersionString == $"10.0.{v.BuildNumber}");
                if (windows1XVersion != null)
                    return windows1XVersion;
                if (Version.TryParse(osVersionString, out var osVersion))
                {
                    if (osVersion.Major == 10 && osVersion.Minor == 0)
                        return new Windows1XVersion(null, null, null, osVersion.Build);
                }
                return null;
            }
        }

        /// <summary>
        /// Transform an operation system name and version to a nice form for summary.
        /// </summary>
        /// <param name="osName">Original operation system name</param>
        /// <param name="osVersion">Original operation system version</param>
        /// <param name="windowsUbr">UBR (Update Build Revision), the revision number of Windows version (if available)</param>
        /// <returns>Prettified operation system title</returns>
        public static string Prettify(string osName, string osVersion, int? windowsUbr = null)
        {
            if (osName == "Windows")
                return PrettifyWindows(osVersion, windowsUbr);
            return $"{osName} {osVersion}";
        }

        private static string PrettifyWindows(string osVersion, int? windowsUbr)
        {
            var windows1XVersion = Windows1XVersion.Resolve(osVersion);
            if (windows1XVersion != null)
                return "Windows " + windows1XVersion.ToPrettifiedString(windowsUbr);

            string brandVersion = WindowsBrandVersions.GetValueOrDefault(osVersion);
            string completeOsVersion = windowsUbr != null && osVersion.Count(c => c == '.') == 2
                ? osVersion + "." + windowsUbr
                : osVersion;
            string fullVersion = brandVersion == null ? osVersion : brandVersion + " (" + completeOsVersion + ")";
            return "Windows " + fullVersion;
        }

        private class MacOSXVersion
        {
            private int DarwinVersion { get; }
            private string CodeName { get; }

            private MacOSXVersion(int darwinVersion, string codeName)
            {
                DarwinVersion = darwinVersion;
                CodeName = codeName;
            }

            private static readonly List<MacOSXVersion> WellKnownVersions = new List<MacOSXVersion>
            {
                new MacOSXVersion(6, "Jaguar"),
                new MacOSXVersion(7, "Panther"),
                new MacOSXVersion(8, "Tiger"),
                new MacOSXVersion(9, "Leopard"),
                new MacOSXVersion(10, "Snow Leopard"),
                new MacOSXVersion(11, "Lion"),
                new MacOSXVersion(12, "Mountain Lion"),
                new MacOSXVersion(13, "Mavericks"),
                new MacOSXVersion(14, "Yosemite"),
                new MacOSXVersion(15, "El Capitan"),
                new MacOSXVersion(16, "Sierra"),
                new MacOSXVersion(17, "High Sierra"),
                new MacOSXVersion(18, "Mojave"),
                new MacOSXVersion(19, "Catalina"),
                new MacOSXVersion(20, "Big Sur"),
                new MacOSXVersion(21, "Monterey"),
                new MacOSXVersion(22, "Ventura"),
                new MacOSXVersion(23, "Sonoma"),
            };

            public static string? ResolveCodeName(string kernelVersion)
            {
                if (string.IsNullOrWhiteSpace(kernelVersion))
                    return null;

                kernelVersion = kernelVersion.ToLowerInvariant().Trim();
                if (kernelVersion.StartsWith("darwin"))
                    kernelVersion = kernelVersion.Substring(6).Trim();
                var numbers = kernelVersion.Split('.');
                if (numbers.Length == 0)
                    return null;

                string majorVersionStr = numbers[0];
                if (int.TryParse(majorVersionStr, out int majorVersion))
                    return WellKnownVersions.FirstOrDefault(v => v.DarwinVersion == majorVersion)?.CodeName;
                return null;
            }
        }

        public static string PrettifyMacOSX(string systemVersion, string kernelVersion)
        {
            string codeName = MacOSXVersion.ResolveCodeName(kernelVersion);
            if (codeName != null)
            {
                int firstDigitIndex = systemVersion.IndexOfAny("0123456789".ToCharArray());
                if (firstDigitIndex == -1)
                    return $"{systemVersion} {codeName} [{kernelVersion}]";

                string systemVersionTitle = systemVersion.Substring(0, firstDigitIndex).Trim();
                string systemVersionNumbers = systemVersion.Substring(firstDigitIndex).Trim();
                return $"{systemVersionTitle} {codeName} {systemVersionNumbers} [{kernelVersion}]";
            }

            return $"{systemVersion} [{kernelVersion}]";
        }
    }
}
