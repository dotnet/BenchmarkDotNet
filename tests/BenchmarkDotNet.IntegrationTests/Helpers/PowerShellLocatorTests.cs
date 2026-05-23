using AwesomeAssertions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Tests.XUnit;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.IntegrationTests.Helpers;

public class PowerShellLocatorTests
{
    [SupportedOSPlatform("windows")]
    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void LocateExeOnWindows()
    {
        // Act
        var exePath = PowerShellLocator.LocateOnWindows();

        // Assert
        exePath.Should().NotBeEmpty();
        var exeName = Path.GetFileName(exePath);

        // On GitHub Actions, PowerShell Core is installed by default.
        if (ContinuousIntegration.IsGitHubActionsOnWindows())
        {
            exeName.Should().Be("pwsh.exe");
        }
        else
        {
            exeName.Should().BeOneOf(
                "powershell.exe", // Windows PowerShell
                "pwsh.exe",       // PowerShell Core
                "powershell");    // Fallback command, which will be resolved via PATH.
        }
    }
}
