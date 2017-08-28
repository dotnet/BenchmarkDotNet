using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class RuntimeInformationTests
    {
        [AppVeyorWithClrOnlyFact]
        public void DetectsHyperVOnAppveyor()
        {
            VirtualMachineHypervisor hypervisor = RuntimeInformation.GetVirtualMachineHypervisor();

            Assert.Equal(hypervisor, HyperV.Default);
        }
    }
}
