using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Microsoft.Win32;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace BenchmarkDotNet.Portability
{
    internal static class RuntimeInformation
    {
        internal const string DebugConfigurationName = "DEBUG";
        internal const string ReleaseConfigurationName = "RELEASE";
        internal const string Unknown = "?";

        // Many of these checks allocate and/or are expensive to compute. We store the results in static readonly fields to keep Engine non-allocating.
        // Static readonly fields are used instead of properties to avoid an extra getter method call that might not be tier1 jitted.
        // This class is internal, so we don't need to expose these as properties.

        /// <summary>
        /// returns true for both the old (implementation of .NET Framework) and new Mono (.NET 6+ flavour)
        /// </summary>
        public static readonly bool IsMono = Type.GetType("Mono.RuntimeStructs") != null;

        public static readonly bool IsOldMono = Type.GetType("Mono.Runtime") != null;

        public static readonly bool IsNewMono = IsMono && !IsOldMono;

        public static readonly bool IsFullFramework =
#if NET6_0_OR_GREATER
            // This could be const, but we want to avoid unreachable code warnings.
            false;
#else
            FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
#endif

        [SupportedOSPlatformGuard("browser")]
#if NET6_0_OR_GREATER
        public static readonly bool IsWasm = OperatingSystem.IsBrowser();
#else
        public static readonly bool IsWasm = IsOSPlatform(OSPlatform.Create("BROWSER"));
#endif

#if NETSTANDARD2_0
        public static readonly bool IsAot = GetIsAot();

        private static bool GetIsAot()
        {
            Type runtimeFeature = Type.GetType("System.Runtime.CompilerServices.RuntimeFeature");
            if (runtimeFeature != null)
            {
                MethodInfo? methodInfo = runtimeFeature.GetProperty("IsDynamicCodeCompiled", BindingFlags.Public | BindingFlags.Static)?.GetMethod;

                if (methodInfo != null)
                {
                    return !(bool)methodInfo.Invoke(null, null);
                }
            }

            if (FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback for old runtimes like legacy MonoAot, test if dynamic method works.
            try
            {
                _ = new System.Reflection.Emit.DynamicMethod("test", typeof(void), []);
                return false;
            }
            catch
            {
                return true;
            }
        }
#else
        public static readonly bool IsAot = !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

        public static bool IsNetCore
            => ((Environment.Version.Major >= 5) || FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase))
                && !IsAot;

        public static bool IsNativeAOT
            => Environment.Version.Major >= 5
               && IsAot
               && !IsWasm && !IsMono; // Wasm and Mono AOT are also AOT

        // File-based apps contains specific RuntimeHostConfigurationOptions.
        // https://github.com/dotnet/dotnet/blob/v10.0.302/src/sdk/documentation/general/dotnet-run-file.md
        public static bool IsFileBasedApp => AppContext.GetData("EntryPointFilePath") != null
                                          && AppContext.GetData("EntryPointFileDirectoryPath") != null;

        public static readonly bool IsRunningInContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

        internal static string GetArchitecture() => GetCurrentPlatform().ToString();

        internal static string GetRuntimeVersion()
        {
            if (IsWasm)
            {
                // code copied from https://github.com/dotnet/runtime/blob/2c573b59aaaf3fd17e2ecab95ad3769f195d2dbc/src/libraries/System.Runtime.InteropServices.RuntimeInformation/src/System/Runtime/InteropServices/RuntimeInformation/RuntimeInformation.cs#L20-L30
                string? versionString = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                // Strip the git hash if there is one
                if (versionString != null)
                {
                    int plusIndex = versionString.IndexOf('+');
                    if (plusIndex != -1)
                    {
                        versionString = versionString.Substring(0, plusIndex);
                    }
                }

                string runtimeName = IsMono ? "Mono" : "CoreCLR";
                return $".NET Core ({runtimeName}) {versionString}";
            }
            else if (IsOldMono)
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                {
                    string? version = monoDisplayName.Invoke(null, null)?.ToString();
                    if (version != null)
                    {
                        int bracket1 = version.IndexOf('('), bracket2 = version.IndexOf(')');
                        if (bracket1 != -1 && bracket2 != -1)
                        {
                            string comment = version.Substring(bracket1 + 1, bracket2 - bracket1 - 1);
                            var commentParts = comment.Split([' '], StringSplitOptions.RemoveEmptyEntries);
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
            if (OsDetector.IsAndroid())
            {
                return $".NET {Environment.Version}";
            }

            return CoreRuntime.TryGetVersion(out var version) && version.Major >= 5
                ? $".NET {version} ({GetDetailedVersion()})"
                : $".NET Core {version?.ToString() ?? Unknown} ({GetDetailedVersion()})";

            string GetDetailedVersion()
            {
                // Single-file publish and NativeAot app have empty assembly location.
                string coreclrLocation = GetCoreLibDllLocation();
                if (coreclrLocation.IsBlank())
                    return CoreRuntime.GetVersionFromFrameworkDescription();
                // .Net Core 2.X has confusing FrameworkDescription like 4.6.X.
                if (version?.Major >= 3)
                    return $"{CoreRuntime.GetVersionFromFrameworkDescription()}, {FileVersionInfo.GetVersionInfo(coreclrLocation).FileVersion}";
                return FileVersionInfo.GetVersionInfo(coreclrLocation).FileVersion!;
            }
        }

        internal static Runtime GetTargetOrCurrentRuntime(Assembly? assembly)
        {
            // Match order of checks in GetCurrentRuntime().
            if (!IsMono && !IsWasm)
            {
                if (IsFullFramework)
                    return ClrRuntime.GetTargetOrCurrentVersion(assembly);
                // 99% of the time the core runtime is the same as the target framework, but the runtime could roll forward if it's not self-contained.
                if (IsNetCore)
                    return CoreRuntime.GetTargetOrCurrentVersion(assembly);
            }
            return GetCurrentRuntime();
        }

        internal static Runtime GetCurrentRuntime()
        {
            //do not change the order of conditions because it may cause incorrect determination of runtime
            if (IsWasm)
                return WasmRuntime.GetCurrentVersion();
            if (IsNewMono)
                // New Mono AOT (MonoAotLLVM) has no official workload, it can only be built by custom runtime artifacts, we don't support it.
                return MonoCoreRuntime.GetCurrentVersion();
            if (IsOldMono)
                return IsAot ? MonoAotRuntime.Default : MonoRuntime.Default;
            if (IsFullFramework)
                return ClrRuntime.GetCurrentVersion();
            if (IsNetCore)
                return CoreRuntime.GetCurrentVersion();
            if (IsNativeAOT)
                return NativeAotRuntime.GetCurrentVersion();

            return UnknownRuntime.Instance;
        }

        public static Platform GetCurrentPlatform()
        {
            // these are not part of .NET Standard 2.0, so we use hardcoded values taken from
            // https://github.com/dotnet/runtime/blob/080fcae7eaa8367abf7900e08ff2e52e3efea5bf/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Architecture.cs#L9
            const Architecture Wasm = (Architecture)4;
            const Architecture S390x = (Architecture)5;
            const Architecture LoongArch64 = (Architecture)6;
            const Architecture Armv6 = (Architecture)7;
            const Architecture Ppc64le = (Architecture)8;
            const Architecture RiscV64 = (Architecture)9;

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
                case RiscV64:
                    return Platform.RiscV64;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool Is64BitPlatform() => IntPtr.Size == 8;

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

        internal static ICollection<Antivirus> GetAntivirusProducts()
        {
            if (!OsDetector.IsWindows())
                return [];

            try
            {
                var command = "ConvertTo-Json @(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | select displayName, pathToSignedProductExe)";

                string? output = ProcessHelper.RunPowerShellCommandAndReadOutput(command);

                if (output.IsBlank())
                    return [];

                var results = JsonSerializer.Deserialize<JsonElement[]>(output)!;
                return results.Select(node =>
                {
                    string name = node.GetProperty("displayName").GetString()!;
                    string path = node.GetProperty("pathToSignedProductExe").GetString()!;
                    return new Antivirus(name, path);
                }).ToList();
            }
            catch
            {
                // Never mind
                return [];
            }
        }

        internal static VirtualMachineHypervisor? GetVirtualMachineHypervisor()
        {
            if (!OsDetector.IsWindows())
                return null;

            VirtualMachineHypervisor[] hypervisors = [HyperV.Default, VirtualBox.Default, VMware.Default];

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS")!;
                string model = key.GetValue("SystemProductName") as string ?? "";
                string manufacturer = key.GetValue("SystemManufacturer") as string ?? "";
                return hypervisors.FirstOrDefault(x => x.IsVirtualMachine(manufacturer, model));
            }
            catch
            {
                // Never mind
                return null;
            }
        }

        // Suppress warning IL3000: 'System.Reflection.Assembly.Location.get' always returns an empty string for assemblies embedded in a single-file app.
        // It's generated by IL Linker, it can't be suppresed `#pragma warning disable IL3000`
        [UnconditionalSuppressMessage(category: "SingleFile", checkId: "IL3000", Justification = "Location property is empty when running with PublishSingleFile/PublishAot")]
        public static string GetCoreLibDllLocation()
            => typeof(object).Assembly.Location;

        // Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier()
        // returns win10-x64, we want the simpler form win-x64
        // the values taken from https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#macos-rids
        internal static string GetPortableRuntimeIdentifier()
        {
            string osPart = OsDetector.IsWindows() ? "win" : (OsDetector.IsMacOS() ? "osx" : "linux");
            string architecture = ProcessArchitecture.ToString().ToLowerInvariant();
            return $"{osPart}-{architecture}";
        }
    }
}