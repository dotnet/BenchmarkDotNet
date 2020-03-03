using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using System;
using System.Linq;

namespace BenchmarkDotNet.Environments
{
    public class CoreRtRuntime : Runtime
    {
        /// <summary>
        /// CoreRT compiled as netcoreapp2.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt20 = new CoreRtRuntime(RuntimeMoniker.CoreRt20, "netcoreapp2.0", "CoreRt 2.0");
        /// <summary>
        /// CoreRT compiled as netcoreapp2.1
        /// </summary>
        public static readonly CoreRtRuntime CoreRt21 = new CoreRtRuntime(RuntimeMoniker.CoreRt21, "netcoreapp2.1", "CoreRt 2.1");
        /// <summary>
        /// CoreRT compiled as netcoreapp2.2
        /// </summary>
        public static readonly CoreRtRuntime CoreRt22 = new CoreRtRuntime(RuntimeMoniker.CoreRt22, "netcoreapp2.2", "CoreRt 2.2");
        /// <summary>
        /// CoreRT compiled as netcoreapp3.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt30 = new CoreRtRuntime(RuntimeMoniker.CoreRt30, "netcoreapp3.0", "CoreRt 3.0");
        /// <summary>
        /// CoreRT compiled as netcoreapp3.1
        /// </summary>
        public static readonly CoreRtRuntime CoreRt31 = new CoreRtRuntime(RuntimeMoniker.CoreRt31, "netcoreapp3.1", "CoreRt 3.1");
        /// <summary>
        /// CoreRT compiled as netcoreapp5.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt50 = new CoreRtRuntime(RuntimeMoniker.CoreRt50, "netcoreapp5.0", "CoreRt 5.0");

        private CoreRtRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }

        internal static CoreRtRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsNetCore && !RuntimeInformation.IsCoreRT)
            {
                throw new NotSupportedException("It's impossible to reliably detect the version of CoreRT if the process is not a .NET Core or CoreRT process!");
            }

            var frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            string netCoreAppVersion = new string(frameworkDescription.SkipWhile(c => !char.IsDigit(c)).ToArray());
            string[] versionNumbers = netCoreAppVersion.Split('.');
            string msBuildMoniker = $"netcoreapp{versionNumbers[0]}.{versionNumbers[1]}";
            string displayName = $"CoreRT {versionNumbers[0]}.{versionNumbers[1]}";

            switch (msBuildMoniker)
            {
                case "netcoreapp2.0": return CoreRt20;
                case "netcoreapp2.1": return CoreRt21;
                case "netcoreapp2.2": return CoreRt22;
                case "netcoreapp3.0": return CoreRt30;
                case "netcoreapp3.1": return CoreRt31;
                case "netcoreapp5.0": return CoreRt50;
                default: // support future version of CoreRT
                    return new CoreRtRuntime(RuntimeMoniker.NotRecognized, msBuildMoniker, displayName);
            }
        }
    }
}
