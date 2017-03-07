#if !UAP
using System;

namespace BenchmarkDotNet.Toolchains.Uap
{
    internal class UapToolchainConfig
    {
        public string DevicePortalUri { get; set; }
        public string Pin { get; set; }
        public string CSRFCookieValue { get; set; }
        public string WMIDCookieValue { get; set; }
        public string UAPBinariesFolder { get; set; }
    }

    internal class UapToolchain : Toolchain, IFormattable
    {
        public UapToolchain(UapToolchainConfig config)
            : base("UAP",
                  new UapGenerator(config.UAPBinariesFolder),
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