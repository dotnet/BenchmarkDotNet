using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Builders;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class HostEnvironmentInfoTests
    {
        [Theory]
        [MemberData(nameof(Hypervisors))]
        public void ReturnsHipervisorNameWhenItsDetected(VirtualMachineHypervisor hypervisor)
        {
            var info = new HostEnvironmentInfoBuilder()
                .WithVMHypervisor(hypervisor)
                .Build();

            string line = info.ToFormattedString().First();

            string expected = $"{HostEnvironmentInfo.BenchmarkDotNetCaption}=v{info.BenchmarkDotNetVersion}, " +
                              $"OS={info.OsVersion.Value}, VM={hypervisor.Name}";
            Assert.Equal(expected, line);
        }

        public static IEnumerable<object[]> Hypervisors()
        {
            yield return new object[] { HyperV.Default };
            yield return new object[] { VirtualBox.Default };
            yield return new object[] { VMware.Default };
        }

        [Fact]
        public void DoesntReturnHypervisorNameWhenItsNotDetected()
        {
            var info = new HostEnvironmentInfoBuilder()
                .WithoutVMHypervisor()
                .Build();

            string line = info.ToFormattedString().First();

            string expected = $"{HostEnvironmentInfo.BenchmarkDotNetCaption}=v{info.BenchmarkDotNetVersion}, " +
                              $"OS={info.OsVersion.Value}";
            Assert.Equal(expected, line);
        }
    }
}
