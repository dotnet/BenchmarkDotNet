using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.System.Power;
using Windows.Win32.System.Threading;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    [SupportedOSPlatform("windows6.1")]
    public static SafeFileHandle PowerCreateRequest(string reason)
    {
        IntPtr reasonPtr = Marshal.StringToHGlobalAuto(reason);
        try
        {
            const uint POWER_REQUEST_CONTEXT_VERSION = 0U;
            var context = new REASON_CONTEXT()
            {
                Version = POWER_REQUEST_CONTEXT_VERSION,
                Flags = POWER_REQUEST_CONTEXT_FLAGS.POWER_REQUEST_CONTEXT_SIMPLE_STRING,
                Reason = new REASON_CONTEXT._Reason_e__Union
                {
                    SimpleReasonString = new Windows.Win32.Foundation.PWSTR(reasonPtr)
                },
            };

            var safePowerHandle = PInvoke.PowerCreateRequest(context);

            if (safePowerHandle.IsInvalid)
                throw new Win32Exception();

            return safePowerHandle;
        }
        finally
        {
            if (reasonPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(reasonPtr);
        }
    }

    [SupportedOSPlatform("windows6.1")]
    private static void PowerSetRequest(SafeFileHandle safePowerHandle, POWER_REQUEST_TYPE requestType)
    {
        if (PInvoke.PowerSetRequest(safePowerHandle, requestType))
            return;

        throw new Win32Exception();
    }

    [SupportedOSPlatform("windows6.1")]
    private static void PowerClearRequest(SafeFileHandle safePowerHandle, POWER_REQUEST_TYPE requestType)
    {
        if (PInvoke.PowerClearRequest(safePowerHandle, requestType))
            return;

        throw new Win32Exception();
    }
}
