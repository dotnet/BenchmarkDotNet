using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class TheoryNetCore30Attribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheoryNetCore30Attribute(string skipReason)
        {
            if (RuntimeInformation.GetCurrentRuntime().TargetFrameworkMoniker != TargetFrameworkMoniker.NetCoreApp30)
                Skip = skipReason;
        }
    }
}