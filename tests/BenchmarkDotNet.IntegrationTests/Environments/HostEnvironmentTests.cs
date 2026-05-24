using AwesomeAssertions;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.IntegrationTests;

public class HostEnvironmentTests
{
    [Fact]
    public void ValidateLazyInitializedValues()
    {
        var info = HostEnvironmentInfo.GetCurrent();

        // Gets lazy initialized values
        var os = info.Os.Value;
        var cpu = info.Cpu.Value;
        var physicalMemory = info.PhysicalMemory;
        var vmHypervisor = info.VirtualMachineHypervisor.Value;
        var antivirusProduct = info.AntivirusProducts.Value;
        var dotnetSdkVersion = info.DotNetSdkVersion.Value;

        // Assert
        os.Should().NotBeNull();
        cpu.Should().NotBeNull();
        physicalMemory.Should().NotBeNull();
        antivirusProduct.Should().NotBeNull();
        dotnetSdkVersion.Should().NotBeNullOrEmpty();

        if (ContinuousIntegration.IsGitHubActionsOnWindows())
            vmHypervisor.Should().NotBeNull();
    }
}
