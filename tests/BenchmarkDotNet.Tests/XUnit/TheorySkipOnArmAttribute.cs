using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit 
{
    public class TheorySkipOnArmAttribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public TheorySkipOnArmAttribute(string skipReason)
        {
            if (RuntimeInformation.GetCurrentPlatform() == Platform.Arm)
                Skip = skipReason;
        }
    }
}