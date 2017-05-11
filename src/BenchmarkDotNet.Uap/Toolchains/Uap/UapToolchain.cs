#if !UAP
using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Uap
{
    internal class UapToolchainConfig
    {
        public string DevicePortalUri { get; internal set; }
        public string Pin { get; internal set; }
        public string CSRFCookieValue { get; internal set; }
        public string WMIDCookieValue { get; internal set; }
        public string UAPBinariesFolder { get; internal set; }
        public string Username { get; internal set; }
        public string Password { get; internal set; }
        public Platform Platform { get; internal set; }
    }

    internal class UapToolchain : Toolchain, IFormattable
    {
        public UapToolchain(UapToolchainConfig config)
            : base("UAP",
                  new UapGenerator(config.UAPBinariesFolder, config.Platform),
                  new UapBuilder(),
                  new UapExecutor(config))
        {
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Empty;
        }
    }
}
#endif