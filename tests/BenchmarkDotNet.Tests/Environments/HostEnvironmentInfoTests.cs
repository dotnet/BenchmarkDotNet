using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Builders;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class HostEnvironmentInfoTests
    {
        [Theory]
        [MemberData(nameof(HypervisorNames))]
        public void ReturnsHypervisorNameWhenItsDetected(string hypervisorName)
        {
            var hypervisor = Hypervisors[hypervisorName];
            var info = new HostEnvironmentInfoBuilder()
                .WithVMHypervisor(hypervisor)
                .Build();

            string line = info.ToFormattedString().First();

            string expected = $"{HostEnvironmentInfo.BenchmarkDotNetCaption} v{info.BenchmarkDotNetVersion}, " +
                              $"{info.OsVersion.Value} ({hypervisor.Name})";
            Assert.Equal(expected, line);
        }

        private static readonly IDictionary<string, VirtualMachineHypervisor> Hypervisors = new Dictionary<string, VirtualMachineHypervisor>
        {
            { HyperV.Default.Name, HyperV.Default },
            { VirtualBox.Default.Name, VirtualBox.Default },
            { VMware.Default.Name, VMware.Default }
        };

        [UsedImplicitly]
        public static TheoryData<string> HypervisorNames => TheoryDataHelper.Create(Hypervisors.Keys);

        [Fact]
        public void DoesntReturnHypervisorNameWhenItsNotDetected()
        {
            var info = new HostEnvironmentInfoBuilder()
                .WithoutVMHypervisor()
                .Build();

            string line = info.ToFormattedString().First();

            string expected = $"{HostEnvironmentInfo.BenchmarkDotNetCaption} v{info.BenchmarkDotNetVersion}, " +
                              $"{info.OsVersion.Value}";
            Assert.Equal(expected, line);
        }
    }
}