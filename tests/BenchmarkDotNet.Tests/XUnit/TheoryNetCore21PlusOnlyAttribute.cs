using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class TheoryNetCore21PlusOnlyAttribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheoryNetCore21PlusOnlyAttribute(string skipReason)
        {
            if (!RuntimeInformation.IsNetCore || NetCoreAppSettings.GetCurrentVersion() == NetCoreAppSettings.NetCoreApp20)
                Skip = skipReason;
        }
    }
}