namespace BenchmarkDotNet.Tests.XUnit;

public enum EnvRequirement
{
    WindowsOnly,
    NonWindows,
    NonWindowsArm,
    NonLinux,
    NonLinuxArm,
    FullFrameworkOnly,
    NonFullFramework,
    DotNetCoreOnly,
    NeedsPrivilegedProcess,
    MonoAotLlvmToolchain
}
