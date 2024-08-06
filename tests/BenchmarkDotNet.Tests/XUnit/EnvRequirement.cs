namespace BenchmarkDotNet.Tests.XUnit;

public enum EnvRequirement
{
    WindowsOnly,
    NonWindows,
    NonLinux,
    FullFrameworkOnly,
    NonFullFramework,
    DotNetCoreOnly
}