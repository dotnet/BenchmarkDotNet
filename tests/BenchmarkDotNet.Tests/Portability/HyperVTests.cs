using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class HyperVTests
    {
        private readonly VirtualMachineHypervisor hypervisor = HyperV.Default;

        [Fact]
        public void ContainsCorrectName()
        {
            Assert.Equal("Hyper-V", hypervisor.Name);
        }

        [Theory]
        [InlineData("Microsoft Corporation", "Virtual Machine", true)]
        [InlineData("microsoft corporation", "virtual machine", true)]
        [InlineData("microsoft", "virtual", true)]
        [InlineData("Dell", "virtual", false)]
        [InlineData("Dell", "ubuntu", false)]
        [InlineData("microsoft corporation", null, false)]
        [InlineData(null, "virtual machine", false)]
        public void DetectsVirtualMachine(string manufacturer, string model, bool expectedResult)
        {
            bool result = hypervisor.IsVirtualMachine(manufacturer, model);
            Assert.Equal(expectedResult, result);
        }
    }
}
