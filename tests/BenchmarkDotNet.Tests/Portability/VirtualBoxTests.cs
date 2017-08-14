using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class VirtualBoxTests
    {
        [Fact]
        public void ContainsCorrectName()
        {
            var hypervisor = new VirtualBox();

            Assert.Equal("VirtualBox", hypervisor.Name);
        }

        [Theory]
        [InlineData("redundant", "virtualbox", true)]
        [InlineData("redundant", "VirtualBox", true)]
        [InlineData("redundant", "vmware", false)]
        [InlineData("redundant", null, false)]
        public void DetectsVirtualMachine(string manufacturer, string model, bool expectedResult)
        {
            var hypervisor = new VirtualBox();

            bool result = hypervisor.IsVirtualMachine(manufacturer, model);
            Assert.Equal(expectedResult, result);
        }
    }
}
