using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Core
{
    /// <summary>
    /// custom settings used in the auto-generated project.json file
    /// </summary>
    public class NetCoreAppSettings
    {
        public static readonly NetCoreAppSettings NetCoreApp11
            = new NetCoreAppSettings(
                "netcoreapp1.1",
                "1.1-*");

        public static readonly NetCoreAppSettings NetCoreApp12
            = new NetCoreAppSettings(
                "netcoreapp1.2",
                "1.2-*");

        public static readonly NetCoreAppSettings NetCoreApp20
            = new NetCoreAppSettings(
                "netcoreapp2.0",
                "2.0-*");

        /// <summary>
        /// <param name="targetFrameworkMoniker">
        /// sample values: netcoreapp1.1, netcoreapp1.2, netcoreapp2.0
        /// </param>
        /// <param name="microsoftNetCoreAppVersion">
        /// used in the auto-generated project.json file, 
        /// "dependencies": { "Microsoft.NETCore.App": { "version": "HERE" } }
        /// </param>
        /// <param name="imports">the custom imports</param>
        /// </summary>
        [PublicAPI]
        public NetCoreAppSettings(
            string targetFrameworkMoniker, 
            string microsoftNetCoreAppVersion, 
            string imports = "[ \"dnxcore50\", \"portable-net45+win8\", \"dotnet5.6\", \"netcore50\" ]")
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            MicrosoftNETCoreAppVersion = microsoftNetCoreAppVersion;
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
    }
}