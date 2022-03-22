using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet.Environments
{
    public class CoreRtRuntime : Runtime
    {
        /// <summary>
        /// CoreRT compiled as net5.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt50 = new CoreRtRuntime(RuntimeMoniker.CoreRt50, "net5.0", "CoreRT 5.0");
        /// <summary>
        /// CoreRT compiled as net6.0
        /// </summary>
        public static readonly CoreRtRuntime CoreRt60 = new CoreRtRuntime(RuntimeMoniker.CoreRt60, "net6.0", "CoreRT 6.0");

        public override bool IsAOT => true;

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
                case Version v when v.Major == 5 && v.Minor == 0: return CoreRt50;
                case Version v when v.Major == 6 && v.Minor == 0: return CoreRt60;
                default:
                    return new CoreRtRuntime(RuntimeMoniker.NotRecognized, $"net{version.Major}.{version.Minor}", $"CoreRT {version.Major}.{version.Minor}");
            }
        }
    }
}
