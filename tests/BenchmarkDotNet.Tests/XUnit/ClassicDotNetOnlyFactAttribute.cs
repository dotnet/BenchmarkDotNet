using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class FactClassicDotNetOnlyAttribute : FactAttribute
    {
        public FactClassicDotNetOnlyAttribute(string skipReason)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            if (!RuntimeInformation.IsFullFramework)
                Skip = skipReason;
        }
    }
}