using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Helpers;
using Microsoft.Win32;
using Perfolizer.Phd.Dto;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Extensions;
using static System.Runtime.InteropServices.RuntimeInformation;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace BenchmarkDotNet.Detectors;

public class OsDetector
{
    public static readonly OsDetector Instance = new ();
    private OsDetector() { }

    internal static string ExecutableExtension => IsWindows() ? ".exe" : string.Empty;
    internal static string ScriptFileExtension => IsWindows() ? ".bat" : ".sh";

    private readonly Lazy<PhdOs> os = new (ResolveOs);
    public static PhdOs GetOs() => Instance.os.Value;

    private static PhdOs ResolveOs()
    {
        if (IsMacOS())
        {
            string systemVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("System Version") ?? "";
            string kernelVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("Kernel Version") ?? "";
            return new PhdOs
            {
                Name = "macOS",
                Version = systemVersion,
                KernelVersion = kernelVersion
            };
        }

        if (IsLinux())
        {
            try
            {
                string version = LinuxOsReleaseHelper.GetNameByOsRelease(File.ReadAllLines("/etc/os-release"));
                bool wsl = IsUnderWsl();
                return new PhdOs
                {
                    Name = "Linux",
                    Version = version,
                    Container = wsl ? "WSL" : null
                };
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        string operatingSystem = RuntimeEnvironment.OperatingSystem;
        string operatingSystemVersion = RuntimeEnvironment.OperatingSystemVersion;
        if (IsWindows())
        {
            int? ubr = GetWindowsUbr();
            if (ubr != null)
                operatingSystemVersion += $".{ubr}";
        }
        return new PhdOs
        {
            Name = operatingSystem,
            Version = operatingSystemVersion
        };
    }

    private static bool IsUnderWsl()
    {
        if (!IsLinux())
            return false;
        try
        {
            return File.Exists("/proc/sys/fs/binfmt_misc/WSLInterop"); // https://superuser.com/a/1749811
        }
        catch (Exception)
        {
            return false;
        }
    }

    // TODO: Introduce a common util API for registry calls, use it also in BenchmarkDotNet.Toolchains.CsProj.GetCurrentVersionBasedOnWindowsRegistry
    /// <summary>
    /// On Windows, this method returns UBR (Update Build Revision) based on Registry.
    /// Returns null if the value is not available
    /// </summary>
    /// <returns></returns>
    private static int? GetWindowsUbr()
    {
        if (IsWindows())
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (var ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (ndpKey == null)
                        return null;

                    return Convert.ToInt32(ndpKey.GetValue("UBR"));
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        return null;
    }

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatformGuard("windows")]
#endif
    internal static bool IsWindows() =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsWindows(); // prefer linker-friendly OperatingSystem APIs
#else
        IsOSPlatform(OSPlatform.Windows);
#endif

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatformGuard("linux")]
#endif
    internal static bool IsLinux() =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsLinux();
#else
        IsOSPlatform(OSPlatform.Linux);
#endif

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatformGuard("macos")]
#endif
    // ReSharper disable once InconsistentNaming
    internal static bool IsMacOS() =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsMacOS();
#else
        IsOSPlatform(OSPlatform.OSX);
#endif

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatformGuard("android")]
#endif
    internal static bool IsAndroid() =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsAndroid();
#else
        Type.GetType("Java.Lang.Object, Mono.Android") != null;
#endif

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatformGuard("ios")]
#endif
    // ReSharper disable once InconsistentNaming
    internal static bool IsIOS() =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsIOS();
#else
        Type.GetType("Foundation.NSObject, Xamarin.iOS") != null;
#endif

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatformGuard("tvos")]
#endif
    // ReSharper disable once InconsistentNaming
    internal static bool IsTvOS() =>
#if NET6_0_OR_GREATER
        OperatingSystem.IsTvOS();
#else
        IsOSPlatform(OSPlatform.Create("TVOS"));
#endif
}