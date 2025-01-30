using Microsoft.Win32.SafeHandles;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    private sealed class SafePowerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafePowerHandle() : base(true) { }

        protected override bool ReleaseHandle() => PInvoke.CloseHandle(handle);
    }
}