using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Toolchains.CsProj;
using JetBrains.Annotations;
using System.Management;

namespace BenchmarkDotNet.Portability
{
    internal static class RuntimeInformation
    {
        private static readonly bool isMono = Type.GetType("Mono.Runtime") != null; // it allocates a lot of memory, we need to check it once in order to keep Enging non-allocating!

        private const string DebugConfigurationName = "DEBUG";
        internal const string ReleaseConfigurationName = "RELEASE";
        internal const string Unknown = "?";

        public static bool IsMono => isMono;

        public static bool IsFullFramework =>
#if CLASSIC
            true;
#else
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
#endif

        public static bool IsNetNative =>
#if CLASSIC
            false;
#else
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);
#endif

        public static bool IsNetCore =>
#if CLASSIC
            false;
#else
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
#endif
        

        internal static string ExecutableExtension => IsWindows() ? ".exe" : string.Empty;

        internal static string ScriptFileExtension => IsWindows() ? ".bat" : ".sh";

        internal static string GetArchitecture() => IntPtr.Size == 4 ? "32bit" : "64bit";

        internal static bool IsWindows()
        {
#if CLASSIC
            return System.Environment.OSVersion.Platform.ToString().Contains("Win");
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }

        internal static bool IsLinux()
        {
#if CLASSIC
            return System.Environment.OSVersion.Platform == PlatformID.Unix
                   && GetSysnameFromUname().Equals("Linux", StringComparison.InvariantCultureIgnoreCase);
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif
        }

        internal static bool IsMacOSX()
        {
#if CLASSIC
            return System.Environment.OSVersion.Platform == PlatformID.Unix
                   && GetSysnameFromUname().Equals("Darwin", StringComparison.InvariantCultureIgnoreCase);
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
        }

        public static string GetOsVersion()
        {
            if (IsMacOSX())
            {
                string systemVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("System Version") ?? "";
                string kernelVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("Kernel Version") ?? "";
                if (!string.IsNullOrEmpty(systemVersion) && !string.IsNullOrEmpty(kernelVersion))
                    return OsBrandStringHelper.PrettifyMacOSX(systemVersion, kernelVersion);
            }

            return OsBrandStringHelper.Prettify(
                Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystem,
                Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion,
                GetWindowsUbr());
        }

        // TODO: Introduce a common util API for registry calls, use it also in BenchmarkDotNet.Toolchains.CsProj.GetCurrentVersionBasedOnWindowsRegistry
        /// <summary>
        /// On Windows, this method returns UBR (Update Build Revision) based on Registry.
        /// Returns null if the value is not available
        /// </summary>
        /// <returns></returns>
        [CanBeNull]
        private static int? GetWindowsUbr()
        {
            if (IsWindows())
            {
                try
                {
                    using (var ndpKey = Microsoft.Win32.RegistryKey
                        .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                        .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
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
            if (IsMacOSX())
                return SysctlCpuInfoProvider.SysctlCpuInfo.Value;

            return null;
        }

        public static string GetNetCoreVersion()
        {
            var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
                return assemblyPath[netCoreAppIndex + 1];
            return null;
        }

        internal static string GetRuntimeVersion()
        {
            if (IsMono)
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                {
                    string version = monoDisplayName.Invoke(null, null)?.ToString();
                    if (version != null)
                    {
                        int bracket1 = version.IndexOf('('), bracket2 = version.IndexOf(')');
                        if (bracket1 != -1 && bracket2 != -1)
                        {
                            string comment = version.Substring(bracket1 + 1, bracket2 - bracket1 - 1);
                            var commentParts = comment.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (commentParts.Length > 2)
                                version = version.Substring(0, bracket1) + "(" + commentParts[0] + " " + commentParts[1] + ")";
                        }
                    }

                    return "Mono " + version;
                }
            }
            else if (IsFullFramework)
            {
                string frameworkVersion = CsProjClassicNetToolchain.GetCurrentNetFrameworkVersion();
                string clrVersion = Environment.Version.ToString();
                return $".NET Framework {frameworkVersion} (CLR {clrVersion})";
            }

            var runtimeVersion = GetNetCoreVersion() ?? "?";

            var coreclrAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).GetTypeInfo().Assembly.Location);
            var corefxAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(Regex).GetTypeInfo().Assembly.Location);

            return $".NET Core {runtimeVersion} (CoreCLR {coreclrAssemblyInfo.FileVersion}, CoreFX {corefxAssemblyInfo.FileVersion})";
        }

        internal static Runtime GetCurrentRuntime()
        {
            if (IsFullFramework)
                return Runtime.Clr;
            if (IsNetCore)
                return Runtime.Core;
            if (IsMono)
                return Runtime.Mono;
            
            throw new NotSupportedException("Unknown .NET Framework"); // todo: adam sitnik fix it
        }

        public static Platform GetCurrentPlatform() => IntPtr.Size == 4 ? Platform.X86 : Platform.X64;

        internal static IEnumerable<JitModule> GetJitModules()
        {
            return
                Process.GetCurrentProcess().Modules
                    .OfType<ProcessModule>()
                    .Where(module => module.ModuleName.Contains("jit"))
                    .Select(module => new JitModule(Path.GetFileNameWithoutExtension(module.FileName), module.FileVersionInfo.ProductVersion));
        }

        internal static string GetJitModulesInfo() => string.Join(";", GetJitModules().Select(m => m.Name + "-v" + m.Version));

        internal static bool HasRyuJit()
        {
            if (IsMono)
                return false;
            if (IsNetCore)
                return true;

            return IntPtr.Size == 8
                   && GetConfiguration() != DebugConfigurationName
                   && !new JitHelper().IsMsX64();
        }

        internal static Jit GetCurrentJit() => HasRyuJit() ? Jit.RyuJit : Jit.LegacyJit;

        internal static string GetJitInfo()
        {
            if (IsMono)
                return ""; // There is no helpful information about JIT on Mono
            if (IsNetCore)
                return "RyuJIT"; // CoreCLR supports only RyuJIT

            // We are working on Full CLR, so there are only LegacyJIT and RyuJIT
            var modules = GetJitModules().ToArray();
            string jitName = HasRyuJit() ? "RyuJIT" : "LegacyJIT";
            if (modules.Length == 1)
            {
                // If we have only one JIT module, we know the version of the current JIT compiler
                return jitName + "-v" + modules[0].Version;
            }
            else
            {
                // Otherwise, let's just print information about all modules
                return jitName + "/" + GetJitModulesInfo();
            }
        }

        internal static IntPtr GetCurrentAffinity() => Process.GetCurrentProcess().TryGetAffinity() ?? default;

        internal static string GetConfiguration()
        {
            bool? isDebug = Assembly.GetEntryAssembly().IsDebug();
            if (isDebug.HasValue == false)
            {
                return Unknown;
            }
            return isDebug.Value ? DebugConfigurationName : ReleaseConfigurationName;
        }

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
        private class JitHelper
        {
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                var value = 0;
                for (int i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (int j = 0; j < 2 * step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }

        public class JitModule
        {
            public string Name { get; }
            public string Version { get; }

            public JitModule(string name, string version)
            {
                Name = name;
                Version = version;
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int uname(IntPtr buf);

        private static string GetSysnameFromUname()
        {
            var buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                int rc = uname(buf);
                if (rc != 0)
                {
                    throw new Exception("uname from libc returned " + rc);
                }

                string os = Marshal.PtrToStringAnsi(buf);
                return os;
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
        }

        internal static ICollection<Antivirus> GetAntivirusProducts()
        {
            var products = new List<Antivirus>();
            if (IsWindows())
            {
                try
                {
                    var wmi = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                    ManagementObjectCollection data = wmi.Get();

                    foreach (ManagementBaseObject o in data)
                    {
                        var av = (ManagementObject)o;
                        if (av != null)
                        {
                            string name = av["displayName"].ToString();
                            string path = av["pathToSignedProductExe"].ToString();
                            products.Add(new Antivirus(name, path));
                        }
                    }
                }
                catch { }
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
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                    {
                        using (ManagementObjectCollection items = searcher.Get())
                        {
                            foreach (ManagementBaseObject item in items)
                            {
                                string manufacturer = item["Manufacturer"]?.ToString();
                                string model = item["Model"]?.ToString();
                                return hypervisors.FirstOrDefault(x => x.IsVirtualMachine(manufacturer, model));
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
}