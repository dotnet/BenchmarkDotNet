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
        public static readonly CoreRtRuntime CoreRt20 = new CoreRtRuntime(RuntimeMoniker.CoreRt20, "netcoreapp2.0", "CoreRT 2.0");
        /// <summary>
        /// CoreRT compiled as netcoreapp2.1
        /// </summary>
        public static readonly CoreRtRuntime CoreRt21 = new CoreRtRuntime(RuntimeMoniker.CoreRt21, "netcoreapp2.1", "CoreRT 2.1");
        /// <summary>
        /// CoreRT compiled as netcoreapp2.2
        /// </summary>
        public static readonly CoreRtRuntime CoreRt22 = new CoreRtRuntime(RuntimeMoniker.CoreRt22, "netcoreapp2.2", "CoreRT 2.2");
        /// <summary>
        /// CoreRT compiled as netcoreapp3.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt30 = new CoreRtRuntime(RuntimeMoniker.CoreRt30, "netcoreapp3.0", "CoreRT 3.0");
        /// <summary>
        /// CoreRT compiled as netcoreapp3.1
        /// </summary>
        public static readonly CoreRtRuntime CoreRt31 = new CoreRtRuntime(RuntimeMoniker.CoreRt31, "netcoreapp3.1", "CoreRT 3.1");
        /// <summary>
        /// CoreRT compiled as net5.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt50 = new CoreRtRuntime(RuntimeMoniker.CoreRt50, "net5.0", "CoreRT 5.0");
        /// <summary>
        /// CoreRT compiled as net6.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt60 = new CoreRtRuntime(RuntimeMoniker.CoreRt60, "net6.0", "CoreRT 6.0");
        /// <summary>
        /// CoreRT compiled as net7.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt70 = new CoreRtRuntime(RuntimeMoniker.CoreRt70, "net7.0", "CoreRT 7.0");

        private CoreRtRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }

        public static CoreRtRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsNetCore && !RuntimeInformation.IsCoreRT)
            {
                throw new NotSupportedException("It's impossible to reliably detect the version of CoreRT if the process is not a .NET Core or CoreRT process!");
            }

            if (!CoreRuntime.TryGetVersion(out var version))
            {
                throw new NotSupportedException("Failed to recognize CoreRT version");
            }

            switch (version)
            {
                case Version v when v.Major == 2 && v.Minor == 0: return CoreRt20;
                case Version v when v.Major == 2 && v.Minor == 1: return CoreRt21;
                case Version v when v.Major == 2 && v.Minor == 2: return CoreRt22;
                case Version v when v.Major == 3 && v.Minor == 0: return CoreRt30;
                case Version v when v.Major == 3 && v.Minor == 1: return CoreRt31;
                case Version v when v.Major == 5 && v.Minor == 0: return CoreRt50;
                case Version v when v.Major == 6 && v.Minor == 0: return CoreRt60;
                case Version v when v.Major == 7 && v.Minor == 0: return CoreRt70;
                default:
                    return new CoreRtRuntime(RuntimeMoniker.NotRecognized, $"net{version.Major}.{version.Minor}", $"CoreRT {version.Major}.{version.Minor}");
            }
        }
    }
}
