namespace BenchmarkDotNet.Tests.XUnit;

public enum EnvRequirement
{
    WindowsOnly,
    NonWindows,
    NonWindowsArm,
    NonLinux,
    FullFrameworkOnly,
    NonFullFramework,
    DotNetCoreOnly,
    NeedsPrivilegedProcess
}