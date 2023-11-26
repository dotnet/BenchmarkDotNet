using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class VirtualBoxTests
    {
        private readonly VirtualMachineHypervisor hypervisor = VirtualBox.Default;

        [Fact]
        public void ContainsCorrectName()
        {
            Assert.Equal("VirtualBox", hypervisor.Name);
        }

        [Theory]
        [InlineData("redundant", "virtualbox", true)]
        [InlineData("redundant", "VirtualBox", true)]
        [InlineData("redundant", "vmware", false)]
        [InlineData("redundant", null, false)]
        public void DetectsVirtualMachine(string manufacturer, string? model, bool expectedResult)
        {
            bool result = hypervisor.IsVirtualMachine(manufacturer, model);
            Assert.Equal(expectedResult, result);
        }
    }
}
