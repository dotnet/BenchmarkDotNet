using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class UapJobAttribute : JobConfigBaseAttribute
    {
        public UapJobAttribute(string devicePortalUri, string csfrCookie, string wmidCookie, string uapBinariesPath)
            : base(new Job("Uap", new EnvMode(new UapRuntime("Uap", devicePortalUri, csfrCookie, wmidCookie, uapBinariesPath)).Freeze()).Freeze())
        {
        }

        public UapJobAttribute(string name, string devicePortalUri, string csfrCookie, string wmidCookie, string uapBinariesPath)
            : base(new Job(name, new EnvMode(new UapRuntime(name, devicePortalUri, csfrCookie, wmidCookie, uapBinariesPath)).Freeze()).Freeze())
        {
        }
    }
}
