using Xunit;
using RuntimeInformation=BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class FactWindowsOnlyAttribute : FactAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public FactWindowsOnlyAttribute(string nonWindowsSkipReason)
        {
            if (!RuntimeInformation.IsWindows())
                Skip = nonWindowsSkipReason;
        }
    }
}