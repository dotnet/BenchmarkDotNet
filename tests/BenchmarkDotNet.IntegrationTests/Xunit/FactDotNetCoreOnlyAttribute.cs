using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Xunit
{
    public class FactDotNetCoreOnlyAttribute : FactAttribute
    {
        public FactDotNetCoreOnlyAttribute(string skipReason)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            if (!RuntimeInformation.IsNetCore)
                Skip = skipReason;
        }
    }
}