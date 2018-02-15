using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class CustomCoreClrToolchain : Toolchain
    {
        /// <summary>
        /// creates toolchain which uses dotnet cli publish to generated self-contained app that references given CoreCLR and CoreFX
        /// </summary>
        /// <param name="displayName">the name of the toolchain to be displayed in results</param>
        /// <param name="coreClrNuGetFeed">path to folder for local CoreCLR builds, url to MyGet feed for previews of CoreCLR. Example: "C:\coreclr\bin\Product\Windows_NT.x64.Debug\.nuget\pkg"</param>
        /// <param name="coreClrVersion">the version of Microsoft.NETCore.Runtime which should be used. Example: "2.1.0-preview2-26305-0"</param>
        /// <param name="coreFxNuGetFeed">path to folder for local CoreFX builds, url to MyGet feed for previews of CoreFX. Example: "C:\Projects\forks\corefx\bin\packages\Debug"</param>
        /// <param name="coreFxVersion">the version of Microsoft.Private.CoreFx.NETCoreApp which should be used. Example: 4.5.0-preview2-26307-0</param>
        /// <param name="targetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        /// <param name="runtimeIdentifier">if not provided, portable OS-arch will be used (example: "win-x64", "linux-x86")</param>
        /// <param name="customDotNetCliPath">if not provided, the default will be used</param>
        /// <returns></returns>
        public CustomCoreClrToolchain(
            string displayName,
            string coreClrNuGetFeed, string coreClrVersion, 
            string coreFxNuGetFeed, string coreFxVersion,
            string targetFrameworkMoniker = "netcoreapp2.1",
            string runtimeIdentifier = null, 
            string customDotNetCliPath = null)
            : base(displayName, 
                  new Generator(coreClrNuGetFeed, coreClrVersion, coreFxNuGetFeed, coreFxVersion, targetFrameworkMoniker, runtimeIdentifier),
                  new Publisher(targetFrameworkMoniker, customDotNetCliPath),
                  new DotNetCliExecutor(customDotNetCliPath))
        {
        }
    }
}
