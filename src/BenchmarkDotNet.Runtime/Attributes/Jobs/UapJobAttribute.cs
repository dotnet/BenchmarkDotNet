using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes.Jobs
{
    /// <summary>
    /// How to obtain these parameters is described in:
    /// http://benchmarkdotnet.org/Advanced/UapJob.htm
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
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

        public UapJobAttribute(string name, string devicePortalUri, string username, string password, string uapBinariesPath, Platform platform)
            : base(new Job(name, new EnvMode(new UapRuntime(name, devicePortalUri, username, password, uapBinariesPath, platform)).Freeze()).Freeze())
        {
        }
    }
}
