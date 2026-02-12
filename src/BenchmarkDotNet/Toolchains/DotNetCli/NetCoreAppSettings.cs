using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using JetBrains.Annotations;

#nullable enable

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    /// <summary>
    /// custom settings used in the auto-generated project.json / .csproj file
    /// </summary>
    [PublicAPI]
    public class NetCoreAppSettings
    {
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp20 = new("netcoreapp2.0", ".NET Core 2.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp21 = new("netcoreapp2.1", ".NET Core 2.1");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp22 = new("netcoreapp2.2", ".NET Core 2.2");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp30 = new("netcoreapp3.0", ".NET Core 3.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp31 = new("netcoreapp3.1", ".NET Core 3.1");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp50 = new("net5.0", ".NET 5.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp60 = new("net6.0", ".NET 6.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp70 = new("net7.0", ".NET 7.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp80 = new("net8.0", ".NET 8.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp90 = new("net9.0", ".NET 9.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp10_0 = new("net10.0", ".NET 10.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp11_0 = new("net11.0", ".NET 11.0");

        /// <summary>
        /// <param name="targetFrameworkMoniker">
        /// sample values: net6.0, net8.0
        /// </param>
        /// <param name="runtimeFrameworkVersion">
        /// used in the auto-generated .csproj file
        /// simply ignored if null or empty
        /// </param>
        /// <param name="name">
        /// display name used for showing the results
        /// </param>
        /// <param name="customDotNetCliPath">
        /// customize dotnet cli path if default is not desired
        /// simply ignored if null or empty
        /// </param>
        /// <param name="packagesPath">the directory to restore packages to</param>
        /// <param name="customRuntimePack">path to a custom runtime pack</param>
        /// <param name="aotCompilerPath">path to Mono AOT compiler</param>
        /// <param name="aotCompilerMode">Mono AOT compiler moder</param>
        /// </summary>
        [PublicAPI]
        public NetCoreAppSettings(
            string targetFrameworkMoniker,
            string runtimeFrameworkVersion,
            string name,
            string customDotNetCliPath = "",
            string packagesPath = "",
            string customRuntimePack = "",
            string aotCompilerPath = "",
            MonoAotCompilerMode aotCompilerMode = MonoAotCompilerMode.mini
            )
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            RuntimeFrameworkVersion = runtimeFrameworkVersion.EnsureNotNull();
            Name = name;

            CustomDotNetCliPath = customDotNetCliPath.EnsureNotNull();
            PackagesPath = packagesPath.EnsureNotNull();
            CustomRuntimePack = customRuntimePack.EnsureNotNull();
            AOTCompilerPath = aotCompilerPath.EnsureNotNull();
            AOTCompilerMode = aotCompilerMode;
        }

        /// <summary>
        /// Internal constructor that accept CommandLineOptions
        /// </summary>
        internal NetCoreAppSettings(
            string targetFrameworkMoniker,
            string runtimeFrameworkVersion,
            string name,
            CommandLineOptions options)
            : this(
                 targetFrameworkMoniker,
                 runtimeFrameworkVersion: runtimeFrameworkVersion,
                 name: name,
                 customDotNetCliPath: options.CliPath?.FullName ?? "",
                 packagesPath: options.RestorePath?.FullName ?? "",
                 customRuntimePack: options.CustomRuntimePack ?? "",
                 aotCompilerPath: options.AOTCompilerPath?.ToString() ?? "",
                 aotCompilerMode: options.AOTCompilerMode)
        {
        }

        internal NetCoreAppSettings(string targetFrameworkMoniker, string name)
            : this(targetFrameworkMoniker, runtimeFrameworkVersion: "", name)
        {
        }

        /// <summary>
        /// sample values: net6.0, net8.0
        /// </summary>
        public string TargetFrameworkMoniker { get; }

        public string RuntimeFrameworkVersion { get; }

        /// <summary>
        /// display name used for showing the results
        /// </summary>
        public string Name { get; }

        public string CustomDotNetCliPath { get; }

        /// <summary>
        /// The directory to restore packages to.
        /// </summary>
        public string PackagesPath { get; }

        /// <summary>
        /// Path to a custom runtime pack.
        /// </summary>
        public string CustomRuntimePack { get; }

        /// <summary>
        /// Path to the Mono AOT Compiler
        /// </summary>
        public string AOTCompilerPath { get; }

        /// <summary>
        /// Mono AOT Compiler mode, either 'mini' or 'llvm'
        /// </summary>
        public MonoAotCompilerMode AOTCompilerMode { get; }

        public NetCoreAppSettings WithCustomDotNetCliPath(string customDotNetCliPath, string? displayName = null)
            => new NetCoreAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, displayName ?? Name, customDotNetCliPath, PackagesPath);

        public NetCoreAppSettings WithCustomPackagesRestorePath(string packagesPath, string? displayName = null)
            => new NetCoreAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, displayName ?? Name, CustomDotNetCliPath, packagesPath);
    }
}
