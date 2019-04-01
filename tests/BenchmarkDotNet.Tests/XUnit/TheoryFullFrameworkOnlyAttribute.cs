using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class TheoryFullFrameworkOnlyAttribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheoryFullFrameworkOnlyAttribute(string skipReason)
        {
            if (!RuntimeInformation.IsFullFramework)
                Skip = skipReason;
        }
    }
}