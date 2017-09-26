using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Xunit
{
    public class TheoryWindowsOnlyAttribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheoryWindowsOnlyAttribute(string nonWindowsSkipReason)
        {
            if (!RuntimeInformation.IsWindows())
                Skip = nonWindowsSkipReason;
        }
    }
}