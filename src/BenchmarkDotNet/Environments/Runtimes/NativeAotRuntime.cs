using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet.Environments
{
    public class NativeAotRuntime : Runtime
    {
        /// <summary>
        /// NativeAOT compiled as net7.0
        /// </summary>
        public static readonly NativeAotRuntime NativeAot70 = new NativeAotRuntime(RuntimeMoniker.NativeAot70, "net7.0", "NativeAOT 7.0");

        public override bool IsAOT => true;

        private NativeAotRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }

        public static NativeAotRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsNetCore && !RuntimeInformation.IsNativeAOT)
            {
                throw new NotSupportedException("It's impossible to reliably detect the version of NativeAOT if the process is not a .NET or NativeAOT process!");
            }

            if (!CoreRuntime.TryGetVersion(out var version))
            {
                throw new NotSupportedException("Failed to recognize NativeAOT version");
            }

            switch (version)
            {
                case Version v when v.Major == 7 && v.Minor == 0: return NativeAot70;
                default:
                    return new NativeAotRuntime(RuntimeMoniker.NotRecognized, $"net{version.Major}.{version.Minor}", $"NativeAOT {version.Major}.{version.Minor}");
            }
        }
    }
}
