namespace BenchmarkDotNet.Tests.XUnit;

public enum EnvRequirement
{
    WindowsOnly,
    NonWindows,
    NonLinux,
    FullFrameworkOnly,
    DotNetCoreOnly,
    DotNetCore30Only
}