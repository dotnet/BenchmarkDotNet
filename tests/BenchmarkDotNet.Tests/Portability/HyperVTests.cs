using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class HyperVTests
    {
        [Fact]
        public void ContainsCorrectName()
        {
            var hypervisor = new HyperV();

            Assert.Equal("Hyper-V", hypervisor.Name);
        }

        [Theory]
        [InlineData("Microsoft Corporation", "Virtual Machine", true)]
        [InlineData("microsoft corporation", "virtual machine", true)]
        [InlineData("microsoft", "virtual", true)]
        [InlineData("Dell", "virtual", false)]
        [InlineData("Dell", "ubuntu", false)]
        public void DetectsVirtualMachine(string manufacturer, string model, bool expectedResult)
        {
            var hypervisor = new HyperV();

            bool result = hypervisor.IsVirtualMachine(manufacturer, model);
            Assert.Equal(expectedResult, result);
        }
    }
}
