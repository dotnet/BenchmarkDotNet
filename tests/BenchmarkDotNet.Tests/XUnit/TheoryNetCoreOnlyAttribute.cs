using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class TheoryNetCoreOnlyAttribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheoryNetCoreOnlyAttribute(string noDotNetCoreSkipReason)
        {
            if (!RuntimeInformation.IsNetCore)
                Skip = noDotNetCoreSkipReason;
        }
    }
}