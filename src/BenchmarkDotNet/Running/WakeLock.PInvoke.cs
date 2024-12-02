using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    private static class PInvoke
    {
        public static SafePowerHandle PowerCreateRequest(string reason)
        {
            REASON_CONTEXT context = new REASON_CONTEXT()
            {
                Version = POWER_REQUEST_CONTEXT_VERSION,
                Flags = POWER_REQUEST_CONTEXT_FLAGS.POWER_REQUEST_CONTEXT_SIMPLE_STRING,
                SimpleReasonString = reason
            };
            SafePowerHandle safePowerHandle = PowerCreateRequest(context);
            if (safePowerHandle.IsInvalid) { throw new Win32Exception(); }
            return safePowerHandle;
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern SafePowerHandle PowerCreateRequest(REASON_CONTEXT Context);

        public static void PowerSetRequest(SafePowerHandle safePowerHandle, POWER_REQUEST_TYPE requestType)
        {
            if (!InvokePowerSetRequest(safePowerHandle, requestType))
            {
                throw new Win32Exception();
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "PowerSetRequest", ExactSpelling = true, SetLastError = true)]
        private static extern bool InvokePowerSetRequest(SafePowerHandle PowerRequest, POWER_REQUEST_TYPE RequestType);

        public static void PowerClearRequest(SafePowerHandle safePowerHandle, POWER_REQUEST_TYPE requestType)
        {
            if (!InvokePowerClearRequest(safePowerHandle, requestType))
            {
                throw new Win32Exception();
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "PowerClearRequest", ExactSpelling = true, SetLastError = true)]
        private static extern bool InvokePowerClearRequest(SafePowerHandle PowerRequest, POWER_REQUEST_TYPE RequestType);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool CloseHandle(nint hObject);

        private struct REASON_CONTEXT
        {
            public uint Version;

            public POWER_REQUEST_CONTEXT_FLAGS Flags;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string SimpleReasonString;
        }

        private const uint POWER_REQUEST_CONTEXT_VERSION = 0U;

        private enum POWER_REQUEST_CONTEXT_FLAGS : uint
        {
            POWER_REQUEST_CONTEXT_DETAILED_STRING = 2U,
            POWER_REQUEST_CONTEXT_SIMPLE_STRING = 1U,
        }

        public enum POWER_REQUEST_TYPE
        {
            PowerRequestDisplayRequired = 0,
            PowerRequestSystemRequired = 1,
            PowerRequestAwayModeRequired = 2,
            PowerRequestExecutionRequired = 3,
        }
    }
}