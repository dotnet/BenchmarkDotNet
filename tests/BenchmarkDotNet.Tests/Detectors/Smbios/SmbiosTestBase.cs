using BenchmarkDotNet.Detectors;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public class SmbiosTestBase
{
    protected readonly ITestOutputHelper Output;

    public SmbiosTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected static bool IsSmbiosAvailable()
    {
        if (OsDetector.IsWindows())
            return true;

#if NET
        if (OsDetector.IsLinux() && Environment.IsPrivilegedProcess)
            return true;
#endif
        // On macos, SMBIOS information is available on macos(x64) only.
        if (OsDetector.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            return true;

        return false;
    }
}
