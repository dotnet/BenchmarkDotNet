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
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp11 = new NetCoreAppSettings("netcoreapp1.1", "1.1-*", ".NET Core 1.1");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp12 = new NetCoreAppSettings("netcoreapp1.2", "1.2-*", ".NET Core 1.0");
        [PublicAPI] public static readonly NetCoreAppSettings NetCoreApp20 = new NetCoreAppSettings("netcoreapp2.0", "2.0-*", ".NET Core 2.0");

        private static NetCoreAppSettings Default => NetCoreApp11;

        /// <summary>
        /// <param name="targetFrameworkMoniker">
        /// sample values: netcoreapp1.1, netcoreapp1.2, netcoreapp2.0
        /// </param>
        /// <param name="microsoftNetCoreAppVersion">
        /// used in the auto-generated project.json file, 
        /// "dependencies": { "Microsoft.NETCore.App": { "version": "HERE" } }
        /// </param>
        /// <param name="name">display name used for showing the results</param>
        /// <param name="imports">the custom imports</param>
        /// </summary>
        [PublicAPI]
        public NetCoreAppSettings(
            string targetFrameworkMoniker, 
            string microsoftNetCoreAppVersion, 
            string name,
            string imports = "[ \"dnxcore50\", \"portable-net45+win8\", \"dotnet5.6\", \"netcore50\" ]")
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            MicrosoftNETCoreAppVersion = microsoftNetCoreAppVersion;
            Name = name;
            Imports = imports;
        }

        /// <summary>
        /// sample values: netcoreapp1.1, netcoreapp1.2, netcoreapp2.0
        /// </summary>
        public string TargetFrameworkMoniker { get; }

        /// <summary>
        /// "dependencies": { "Microsoft.NETCore.App": { "version": "THIS" } }
        /// </summary>
        public string MicrosoftNETCoreAppVersion { get; }

        /// <summary>
        /// the custom imports
        /// </summary>
        public string Imports { get; }

        /// <summary>
        /// display name used for showing the results
        /// </summary>
        public string Name { get; }

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