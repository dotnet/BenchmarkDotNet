using System.Reflection;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    /// <summary>
    /// custom settings used in the auto-generated project.json / .csproj file
    /// </summary>
    [PublicAPI]
    public class NetCoreAppSettings
    {
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp11 = new NetCoreAppSettings("netcoreapp1.1", null, ".NET Core 1.1");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp12 = new NetCoreAppSettings("netcoreapp1.2", null, ".NET Core 1.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp20 = new NetCoreAppSettings("netcoreapp2.0", null, ".NET Core 2.0");

        private static NetCoreAppSettings Default =>
#if NETCOREAPP2_0
            NetCoreApp20;
#else
            NetCoreApp11;
#endif

        /// <summary>
        /// <param name="targetFrameworkMoniker">
        /// sample values: netcoreapp1.1, netcoreapp1.2, netcoreapp2.0
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
        /// simply ignored if null
        /// </param>
        /// </summary>
        [PublicAPI]
        public NetCoreAppSettings(
            string targetFrameworkMoniker, 
            string runtimeFrameworkVersion, 
            string name,
            string customDotNetCliPath = null)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            RuntimeFrameworkVersion = runtimeFrameworkVersion;
            Name = name;
            CustomDotNetCliPath = customDotNetCliPath;
        }

        /// <summary>
        /// sample values: netcoreapp1.1, netcoreapp2.0, netcoreapp2.1
        /// </summary>
        public string TargetFrameworkMoniker { get; }

        public string RuntimeFrameworkVersion { get; }

        /// <summary>
        /// display name used for showing the results
        /// </summary>
        public string Name { get; }

        public string CustomDotNetCliPath { get; }

        public NetCoreAppSettings WithCustomDotNetCliPath(string customDotNetCliPath, string displayName)
            => new NetCoreAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, displayName, customDotNetCliPath);

        internal static NetCoreAppSettings GetCurrentVersion()
        {
#if CLASSIC
            return Default;
#else
            try
            {
                // it's an experimental way to determine the .NET Core Runtime version
                // based on dev packages available at https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App
                var assembly = Assembly.Load(new AssemblyName("System.Runtime"));
                if (assembly.FullName.Contains("Version=4.1.1"))
                    return NetCoreApp11;

                // the problem is that both netcoreapp1.2 and netcoreapp2.0 have 
                // "System.Runtime Version=1.2.0.0". 
                // 2.0 was officialy announced name, so let's bet on it (1.2 was probably an internal dev thing)
                if (assembly.FullName.Contains("Version=4.2"))
                    return NetCoreApp20;
            }
            catch
            {
                return Default;
            }

            return Default;
#endif
        }
    }
}