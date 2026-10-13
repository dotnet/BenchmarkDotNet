using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
#if !NET
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using System.Runtime.InteropServices;
using RuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;
#endif

namespace BenchmarkDotNet.Disassemblers;

internal static class CapstoneFactory
{
    internal static CapstoneArm64Disassembler CreateArm64Disassembler(Arm64DisassembleMode disassembleMode)
    {
#if !NET
        _ = FrameworkNativeLibrary.Handle.Value;
#endif
        return CapstoneDisassembler.CreateArm64Disassembler(disassembleMode);
    }

#if !NET
    /// <summary>
    /// .NET Framework does not probe <c>runtimes/{rid}/native</c>, where BenchmarkDotNet.targets copies the native capstone.
    /// Once it is loaded by its full path, the P/Invokes that name the module bind to the loaded library.
    /// When the file is not there, the default search applies - a build for a specific RuntimeIdentifier puts it next to the application.
    /// </summary>
    private static class FrameworkNativeLibrary
    {
        internal static readonly Lazy<IntPtr> Handle = new(Load);

        private static IntPtr Load()
        {
            if (!RuntimeInformation.IsFullFramework)
                return IntPtr.Zero;

            string? rid = RuntimeInformation.GetCurrentPlatform() switch
            {
                Platform.X86 => "win-x86",
                Platform.X64 => "win-x64",
                Platform.Arm64 => "win-arm64",
                _ => null
            };
            var assembly = typeof(CapstoneDisassembler).Assembly;
            string location = ShadowCopyHelper.TryGetOriginalLocation(assembly, out var originalLocation) ? originalLocation : assembly.Location;
            if (rid is null || location.IsBlank())
                return IntPtr.Zero;

            string path = Path.Combine(Path.GetDirectoryName(location)!, "runtimes", rid, "native", "capstone.dll");
            return File.Exists(path) ? LoadLibraryW(path) : IntPtr.Zero;
        }

        [DllImport("kernel32", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string lpLibFileName);
    }
#endif
}
