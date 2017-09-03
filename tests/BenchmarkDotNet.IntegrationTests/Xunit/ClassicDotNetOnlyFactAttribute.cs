using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Xunit
{
    public class FactClassicDotNetOnlyAttribute : FactAttribute
    {
        public FactClassicDotNetOnlyAttribute(string skipReason)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            if (!RuntimeInformation.IsClassic())
                Skip = skipReason;
        }
    }
}