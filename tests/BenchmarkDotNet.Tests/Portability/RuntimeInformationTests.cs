using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class RuntimeInformationTests
    {
        [AppVeyorWithClassicFrameworkOnlyFact]
        public void DetectsHyperVOnAppveyor()
        {
            VirtualMachineHypervisor hypervisor = RuntimeInformation.GetVirtualMachineHypervisor();

            Assert.Equal(hypervisor, HyperV.Default);
        }
    }
}
