using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit {
    public class FactSkipArmAttribute : FactAttribute
    {
        public FactSkipArmAttribute(string skipReason)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            if (RuntimeInformation.GetCurrentPlatform() == Platform.Arm)
                Skip = skipReason;
        }
    }
}