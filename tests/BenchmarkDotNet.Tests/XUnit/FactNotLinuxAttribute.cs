using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class FactNotLinuxAttribute : FactAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public FactNotLinuxAttribute(string linuxSkipReason)
        {
            if (RuntimeInformation.IsLinux())
                Skip = linuxSkipReason;
        }
    }
}