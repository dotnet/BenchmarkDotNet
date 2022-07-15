#if NET6_0_OR_GREATER
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Portability
{
    // Adapted from https://github.com/dotnet/runtime/tree/v5.0.0-preview.5.20278.1/src/installer/managed/Microsoft.DotNet.PlatformAbstractions/Native

    internal static class PlatformApis
    {
        private class DistroInfo
        {
            public string Id;
            public string VersionId;
        }

        private enum Platform
        {
            Unknown = 0,
            Windows = 1,
            Linux = 2,
            Darwin = 3,
            FreeBSD = 4
        }

        private static readonly Lazy<Platform> _platform = new Lazy<Platform>(DetermineOSPlatform);
        private static readonly Lazy<DistroInfo> _distroInfo = new Lazy<DistroInfo>(LoadDistroInfo);

        public static string GetOSName()
        {
            switch (GetOSPlatform())
            {
                case Platform.Windows:
                    return "Windows";
                case Platform.Linux:
                    return GetDistroId() ?? "Linux";
                case Platform.Darwin:
                    return "Mac OS X";
                case Platform.FreeBSD:
                    return "FreeBSD";
                default:
                    return "Unknown";
            }
        }

        public static string GetOSVersion()
        {
            switch (GetOSPlatform())
            {
                case Platform.Windows:
                    return NativeMethods.Windows.RtlGetVersion() ?? string.Empty;
                case Platform.Linux:
                    return GetDistroVersionId() ?? string.Empty;
                case Platform.Darwin:
                    return GetDarwinVersion() ?? string.Empty;
                case Platform.FreeBSD:
                    return GetFreeBSDVersion() ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static string GetDarwinVersion()
        {
            Version version;
            var kernelRelease = NativeMethods.Darwin.GetKernelRelease();
            if (!Version.TryParse(kernelRelease, out version) || version.Major < 5)
            {
                // 10.0 covers all versions prior to Darwin 5
                // Similarly, if the version is not a valid version number, but we have still detected that it is Darwin, we just assume
                // it is OS X 10.0
                return "10.0";
            }
            else
            {
                // Mac OS X 10.1 mapped to Darwin 5.x, and the mapping continues that way
                // So just subtract 4 from the Darwin version.
                // https://en.wikipedia.org/wiki/Darwin_%28operating_system%29
                return $"10.{version.Major - 4}";
            }
        }

        private static string GetFreeBSDVersion()
        {
            // This is same as sysctl kern.version
            // FreeBSD 11.0-RELEASE-p1 FreeBSD 11.0-RELEASE-p1 #0 r306420: Thu Sep 29 01:43:23 UTC 2016     root@releng2.nyi.freebsd.org:/usr/obj/usr/src/sys/GENERIC
            // What we want is major release as minor releases should be compatible.
            try
            {
                // second token up to first dot
                return System.Runtime.InteropServices.RuntimeInformation.OSDescription.Split()[1].Split('.')[0];
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Platform GetOSPlatform()
        {
            return _platform.Value;
        }

        private static string GetDistroId()
        {
            return _distroInfo.Value?.Id;
        }

        private static string GetDistroVersionId()
        {
            return _distroInfo.Value?.VersionId;
        }

        private static DistroInfo LoadDistroInfo()
        {
            DistroInfo result = null;

            // Sample os-release file:
            //   NAME="Ubuntu"
            //   VERSION = "14.04.3 LTS, Trusty Tahr"
            //   ID = ubuntu
            //   ID_LIKE = debian
            //   PRETTY_NAME = "Ubuntu 14.04.3 LTS"
            //   VERSION_ID = "14.04"
            //   HOME_URL = "http://www.ubuntu.com/"
            //   SUPPORT_URL = "http://help.ubuntu.com/"
            //   BUG_REPORT_URL = "http://bugs.launchpad.net/ubuntu/"
            // We use ID and VERSION_ID

            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                result = new DistroInfo();
                foreach (var line in lines)
                {
                    if (line.StartsWith("ID=", StringComparison.Ordinal))
                    {
                        result.Id = line.Substring(3).Trim('"', '\'');
                    }
                    else if (line.StartsWith("VERSION_ID=", StringComparison.Ordinal))
                    {
                        result.VersionId = line.Substring(11).Trim('"', '\'');
                    }
                }
            }

            if (result != null)
            {
                result = NormalizeDistroInfo(result);
            }

            return result;
        }

        // For some distros, we don't want to use the full version from VERSION_ID. One example is
        // Red Hat Enterprise Linux, which includes a minor version in their VERSION_ID but minor
        // versions are backwards compatable.
        //
        // In this case, we'll normalized RIDs like 'rhel.7.2' and 'rhel.7.3' to a generic
        // 'rhel.7'. This brings RHEL in line with other distros like CentOS or Debian which
        // don't put minor version numbers in their VERSION_ID fields because all minor versions
        // are backwards compatible.
        private static DistroInfo NormalizeDistroInfo(DistroInfo distroInfo)
        {
            // Handle if VersionId is null by just setting the index to -1.
            int lastVersionNumberSeparatorIndex = distroInfo.VersionId?.IndexOf('.') ?? -1;

            if (lastVersionNumberSeparatorIndex != -1 && distroInfo.Id == "alpine")
            {
                // For Alpine, the version reported has three components, so we need to find the second version separator
                lastVersionNumberSeparatorIndex = distroInfo.VersionId.IndexOf('.', lastVersionNumberSeparatorIndex + 1);
            }

            if (lastVersionNumberSeparatorIndex != -1 && (distroInfo.Id == "rhel" || distroInfo.Id == "alpine"))
            {
                distroInfo.VersionId = distroInfo.VersionId.Substring(0, lastVersionNumberSeparatorIndex);
            }

            return distroInfo;
        }

        private static Platform DetermineOSPlatform()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return Platform.Windows;
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                return Platform.Linux;
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                return Platform.Darwin;
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Create("FREEBSD")))
            {
                return Platform.FreeBSD;
            }

            return Platform.Unknown;
        }

        private static partial class NativeMethods
        {
            public static class Darwin
            {
                private const int CTL_KERN = 1;
                private const int KERN_OSRELEASE = 2;

                public unsafe static string GetKernelRelease()
                {
                    const uint BUFFER_LENGTH = 32;

                    var name = stackalloc int[2];
                    name[0] = CTL_KERN;
                    name[1] = KERN_OSRELEASE;

                    var buf = stackalloc byte[(int)BUFFER_LENGTH];
                    var len = stackalloc uint[1];
                    *len = BUFFER_LENGTH;

                    try
                    {
                        // If the buffer isn't big enough, it seems sysctl still returns 0 and just sets len to the
                        // necessary buffer size. This appears to be contrary to the man page, but it's easy to detect
                        // by simply checking len against the buffer length.
                        if (sysctl(name, 2, buf, len, IntPtr.Zero, 0) == 0 && *len < BUFFER_LENGTH)
                        {
                            return Marshal.PtrToStringAnsi((IntPtr)buf, (int)*len);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new PlatformNotSupportedException("Error reading Darwin Kernel Version", ex);
                    }
                    throw new PlatformNotSupportedException("Unknown error reading Darwin Kernel Version");
                }

                [DllImport("libc")]
                private unsafe static extern int sysctl(
                    int* name,
                    uint namelen,
                    byte* oldp,
                    uint* oldlenp,
                    IntPtr newp,
                    uint newlen);
            }

            public static class Unix
            {
                public unsafe static string GetUname()
                {
                    // Utsname shouldn't be larger than 2K
                    var buf = stackalloc byte[2048];

                    try
                    {
                        if (uname((IntPtr)buf) == 0)
                        {
                            return Marshal.PtrToStringAnsi((IntPtr)buf);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new PlatformNotSupportedException("Error reading Unix name", ex);
                    }
                    throw new PlatformNotSupportedException("Unknown error reading Unix name");
                }

                [DllImport("libc")]
                private static extern int uname(IntPtr utsname);
            }

            public static class Windows
            {
                [StructLayout(LayoutKind.Sequential)]
                internal struct RTL_OSVERSIONINFOEX
                {
                    internal uint dwOSVersionInfoSize;
                    internal uint dwMajorVersion;
                    internal uint dwMinorVersion;
                    internal uint dwBuildNumber;
                    internal uint dwPlatformId;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                    internal string szCSDVersion;
                }

                // This call avoids the shimming Windows does to report old versions
                [DllImport("ntdll")]
                private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

                internal static string RtlGetVersion()
                {
                    RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
                    osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
                    if (RtlGetVersion(out osvi) == 0)
                    {
                        return $"{osvi.dwMajorVersion}.{osvi.dwMinorVersion}.{osvi.dwBuildNumber}";
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}
#endif
