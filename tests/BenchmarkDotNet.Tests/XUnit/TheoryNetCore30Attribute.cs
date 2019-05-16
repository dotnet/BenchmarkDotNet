using BenchmarkDotNet.Toolchains.DotNetCli;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class TheoryNetCore30Attribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheoryNetCore30Attribute(string skipReason)
        {
            if (NetCoreAppSettings.GetCurrentVersion() != NetCoreAppSettings.NetCoreApp30)
                Skip = skipReason;
        }
    }
}