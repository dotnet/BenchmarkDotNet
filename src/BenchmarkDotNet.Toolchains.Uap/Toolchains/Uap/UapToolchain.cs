#if !UAP
using BenchmarkDotNet.Toolchains.DotNetCli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.Uap
{
    public class UapToolchainConfig
    {
        public string DevicePortalUri { get; set; }
        public string Pin { get; set; }
        public string CSRFCookieValue { get; set; }
        public string WMIDCookieValue { get; set; }
        public string UAPBinariesFolder { get; set; }
    }

    public class UapToolchain : Toolchain, IFormattable
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