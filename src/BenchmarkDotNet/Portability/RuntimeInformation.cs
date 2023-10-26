using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability.Cpu;
using JetBrains.Annotations;
using Microsoft.Win32;
using static System.Runtime.InteropServices.RuntimeInformation;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace BenchmarkDotNet.Portability
{
    internal static class RuntimeInformation
    {
        internal const string DebugConfigurationName = "DEBUG";
        internal const string ReleaseConfigurationName = "RELEASE";
        internal const string Unknown = "?";

        /// <summary>
        /// returns true for both the old (implementation of .NET Framework) and new Mono (.NET 6+ flavour)
        /// </summary>
        public static bool IsMono { get; } = Type.GetType("Mono.RuntimeStructs") != null; // it allocates a lot of memory, we need to check it once in order to keep Engine non-allocating!

        public static bool IsOldMono { get; } = Type.GetType("Mono.Runtime") != null;

        public static bool IsNewMono { get; } = IsMono && !IsOldMono;

        public static bool IsFullFramework =>
#if NET6_0_OR_GREATER
            false;
#else
            FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
#endif

        [PublicAPI]
        public static bool IsNetNative => FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);

        public static bool IsNetCore
            => ((Environment.Version.Major >= 5) || FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(typeof(object).Assembly.Location);

        public static bool IsNativeAOT
            => Environment.Version.Major >= 5
                && string.IsNullOrEmpty(typeof(object).Assembly.Location) // it's merged to a single .exe and .Location returns null
                && !IsWasm; // Wasm also returns "" for assembly locations

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatformGuard("browser")]
#endif
        public static bool IsWasm =>
#if NET6_0_OR_GREATER
            OperatingSystem.IsBrowser();
#else
            IsOSPlatform(OSPlatform.Create("BROWSER"));
#endif

#if NETSTANDARD2_0
        public static bool IsAot { get; } = IsAotMethod(); // This allocates, so we only want to call it once statically.

        private static bool IsAotMethod()
        {
            Type runtimeFeature = Type.GetType("System.Runtime.CompilerServices.RuntimeFeature");
            if (runtimeFeature != null)
            {
                MethodInfo methodInfo = runtimeFeature.GetProperty("IsDynamicCodeCompiled", BindingFlags.Public | BindingFlags.Static)?.GetMethod;

                if (methodInfo != null)
                {
                    return !(bool)methodInfo.Invoke(null, null);
                }
            }

            return false;
        }
#else
        public static bool IsAot => !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

        public static bool IsRunningInContainer => string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

        internal static string ExecutableExtension => IsWindows() ? ".exe" : string.Empty;

        internal static string ScriptFileExtension => IsWindows() ? ".bat" : ".sh";

        internal static string GetArchitecture() => GetCurrentPlatform().ToString();

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
        internal static bool IsIOS() =>
#if NET6_0_OR_GREATER
            OperatingSystem.IsIOS();
#else
            Type.GetType("Foundation.NSObject, Xamarin.iOS") != null;
#endif

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatformGuard("tvos")]
#endif
        internal static bool IsTvOS() =>
#if NET6_0_OR_GREATER
            OperatingSystem.IsTvOS();
#else
            IsOSPlatform(OSPlatform.Create("TVOS"));
#endif

        public static string GetOsVersion()
        {
            if (IsMacOS())
            {
                string systemVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("System Version") ?? "";
                string kernelVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("Kernel Version") ?? "";
                if (!string.IsNullOrEmpty(systemVersion) && !string.IsNullOrEmpty(kernelVersion))
                    return OsBrandStringHelper.PrettifyMacOSX(systemVersion, kernelVersion);
            }

            if (IsLinux())
            {
                string? version = GetLinuxOsVersion();
                if (version != null && version.Trim() != "")
                    return version;
            }

            string operatingSystem = RuntimeEnvironment.OperatingSystem;
            string operatingSystemVersion = RuntimeEnvironment.OperatingSystemVersion;

            return OsBrandStringHelper.Prettify(
                operatingSystem,
                operatingSystemVersion,
                GetWindowsUbr());
        }

        private static string? GetLinuxOsVersion()
        {
            if (!IsLinux())
                return null;
            try
            {
                string version = LinuxOsReleaseHelper.GetNameByOsRelease(File.ReadAllLines("/etc/os-release"));
                bool wsl = IsUnderWsl();
                return wsl ? version + " WSL" : version;
            }
            catch (Exception)
            {
                return null;
            }
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

        internal static CpuInfo GetCpuInfo()
        {
            if (IsWindows() && IsFullFramework && !IsMono)
                return MosCpuInfoProvider.MosCpuInfo.Value;
            if (IsWindows())
                return WmicCpuInfoProvider.WmicCpuInfo.Value;
            if (IsLinux())
                return ProcCpuInfoProvider.ProcCpuInfo.Value;
            if (IsMacOS())
                return SysctlCpuInfoProvider.SysctlCpuInfo.Value;

            return null;
        }

        internal static string GetRuntimeVersion()
        {
            if (IsWasm)
            {
                // code copied from https://github.com/dotnet/runtime/blob/2c573b59aaaf3fd17e2ecab95ad3769f195d2dbc/src/libraries/System.Runtime.InteropServices.RuntimeInformation/src/System/Runtime/InteropServices/RuntimeInformation/RuntimeInformation.cs#L20-L30
                string versionString = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                // Strip the git hash if there is one
                if (versionString != null)
                {
                    int plusIndex = versionString.IndexOf('+');
                    if (plusIndex != -1)
                    {
                        versionString = versionString.Substring(0, plusIndex);
                    }
                }

                return $".NET Core (Mono) {versionString}";
            }
            else if (IsOldMono)
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                {
                    string version = monoDisplayName.Invoke(null, null)?.ToString();
                    if (version != null)
                    {
                        int bracket1 = version.IndexOf('('), bracket2 = version.IndexOf(')');
                        if (bracket1 != -1 && bracket2 != -1)
                        {
                            string comment = version.Substring(bracket1 + 1, bracket2 - bracket1 - 1);
                            var commentParts = comment.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (commentParts.Length > 2)
                                version = version.Substring(0, bracket1) + "(" + commentParts[0] + " " + commentParts[1] + ")";
                        }
                    }

                    return "Mono " + version;
                }
            }
            else if (IsNewMono)
            {
                return $"{GetNetCoreVersion()} using MonoVM";
            }
            else if (IsFullFramework)
            {
                return FrameworkVersionHelper.GetFrameworkDescription();
            }
            else if (IsNetCore)
            {
                return GetNetCoreVersion();
            }
            else if (IsNativeAOT)
            {
                return FrameworkDescription;
            }

            return Unknown;
        }

        private static string GetNetCoreVersion()
        {
            if (IsAndroid())
            {
                return $".NET {Environment.Version}";
            }
            else
            {
                var coreclrAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).GetTypeInfo().Assembly.Location);
                var corefxAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(Regex).GetTypeInfo().Assembly.Location);

                if (CoreRuntime.TryGetVersion(out var version) && version >= new Version(5, 0))
                {
                    // after the merge of dotnet/corefx and dotnet/coreclr into dotnet/runtime the version should always be the same
                    Debug.Assert(coreclrAssemblyInfo.FileVersion == corefxAssemblyInfo.FileVersion);

                    return $".NET {version} ({coreclrAssemblyInfo.FileVersion})";
                }
                else
                {
                    string runtimeVersion = version != default ? version.ToString() : Unknown;

                    return $".NET Core {runtimeVersion} (CoreCLR {coreclrAssemblyInfo.FileVersion}, CoreFX {corefxAssemblyInfo.FileVersion})";
                }
            }
        }

        internal static Runtime GetCurrentRuntime()
        {
            //do not change the order of conditions because it may cause incorrect determination of runtime
            if (IsAot && IsMono)
                return MonoAotLLVMRuntime.Default;
            if (IsWasm)
                return WasmRuntime.Default;
            if (IsNewMono)
                return MonoRuntime.GetCurrentVersion();
            if (IsOldMono)
                return MonoRuntime.Default;
            if (IsFullFramework)
                return ClrRuntime.GetCurrentVersion();
            if (IsNetCore)
                return CoreRuntime.GetCurrentVersion();
            if (IsNativeAOT)
                return NativeAotRuntime.GetCurrentVersion();

            throw new NotSupportedException("Unknown .NET Runtime");
        }

        public static Platform GetCurrentPlatform()
        {
            // these are not part of .NET Standard 2.0, so we use hardcoded values taken from
            // https://github.com/dotnet/runtime/blob/1a37caf773a3b857ccff49a31be3333d4fdc491f/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Architecture.cs#L9
            const Architecture Wasm = (Architecture)4;
            const Architecture S390x = (Architecture)5;
            const Architecture LoongArch64 = (Architecture)6;
            const Architecture Armv6 = (Architecture)7;
            const Architecture Ppc64le = (Architecture)8;

            switch (ProcessArchitecture)
            {
                case Architecture.Arm:
                    return Platform.Arm;
                case Architecture.Arm64:
                    return Platform.Arm64;
                case Architecture.X64:
                    return Platform.X64;
                case Architecture.X86:
                    return Platform.X86;
                case Wasm:
                    return Platform.Wasm;
                case S390x:
                    return Platform.S390x;
                case LoongArch64:
                    return Platform.LoongArch64;
                case Armv6:
                    return Platform.Armv6;
                case Ppc64le:
                    return Platform.Ppc64le;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool Is64BitPlatform() => IntPtr.Size == 8;

        internal static bool HasRyuJit()
        {
            if (IsMono)
                return false;
            if (IsNetCore)
                return true;

            return Is64BitPlatform()
                   && GetConfiguration() != DebugConfigurationName
                   && !new JitHelper().IsMsX64();
        }

        internal static Jit GetCurrentJit() => HasRyuJit() ? Jit.RyuJit : Jit.LegacyJit;

        internal static string GetJitInfo()
        {
            if (IsNativeAOT)
                return "NativeAOT";
            if (IsNetNative || IsAot)
                return "AOT";
            if (IsMono || IsWasm)
                return ""; // There is no helpful information about JIT on Mono
            if (IsNetCore || HasRyuJit()) // CoreCLR supports only RyuJIT
                return "RyuJIT";
            if (IsFullFramework)
                return  "LegacyJIT";

            return Unknown;
        }

        internal static IntPtr GetCurrentAffinity() => Process.GetCurrentProcess().TryGetAffinity() ?? default;

        internal static string GetConfiguration()
        {
            var isDebug = Assembly.GetEntryAssembly().IsDebug();
            if (isDebug.HasValue == false)
            {
                return Unknown;
            }
            return isDebug.Value ? DebugConfigurationName : ReleaseConfigurationName;
        }

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
        private class JitHelper
        {
            [SuppressMessage("IDE0052", "IDE0052")]
            [SuppressMessage("IDE0079", "IDE0079")]
            [SuppressMessage("ReSharper", "NotAccessedField.Local")]
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                int value = 0;
                for (int i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (int j = 0; j < 2 * step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }

        private class JitModule
        {
            public string Name { get; }
            public string Version { get; }

            public JitModule(string name, string version)
            {
                Name = name;
                Version = version;
            }
        }

        internal static ICollection<Antivirus> GetAntivirusProducts()
        {
            var products = new List<Antivirus>();
            if (IsWindows())
            {
                try
                {
                    using (var wmi = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                    using (var data = wmi.Get())
                        foreach (var o in data)
                        {
                            var av = (ManagementObject) o;
                            if (av != null)
                            {
                                string name = av["displayName"].ToString();
                                string path = av["pathToSignedProductExe"].ToString();
                                products.Add(new Antivirus(name, path));
                            }
                        }
                }
                catch
                {
                    // Never mind
                }
            }

            return products;
        }

        internal static VirtualMachineHypervisor GetVirtualMachineHypervisor()
        {
            VirtualMachineHypervisor[] hypervisors = { HyperV.Default, VirtualBox.Default, VMware.Default };

            if (IsWindows())
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                    {
                        using (var items = searcher.Get())
                        {
                            foreach (var item in items)
                            {
                                string manufacturer = item["Manufacturer"]?.ToString();
                                string model = item["Model"]?.ToString();
                                return hypervisors.FirstOrDefault(x => x.IsVirtualMachine(manufacturer, model));
                            }
                        }
                    }
                }
                catch
                {
                    // Never mind
                }
            }

            return null;
        }
    }
}
