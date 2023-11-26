using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class VMwareTests
    {
        private readonly VirtualMachineHypervisor hypervisor = VMware.Default;

        [Fact]
        public void ContainsCorrectName()
        {
            Assert.Equal("VMware", hypervisor.Name);
        }

        [Theory]
        [InlineData("VMWare Inc", "VMWare", true)]
        [InlineData("VMWare Inc", "vmWare", true)]
        [InlineData("redundant", "vmWare", true)]
        [InlineData(null, "vmWare", true)]
        [InlineData("VMWare Inc", "redundant", false)]
        [InlineData("VMWare Inc", null, false)]
        public void DetectsVirtualMachine(string? manufacturer, string? model, bool expectedResult)
        {
            bool result = hypervisor.IsVirtualMachine(manufacturer, model);
            Assert.Equal(expectedResult, result);
        }
    }
}
