using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    private static class PInvoke
    {
        public static SafePowerHandle PowerCreateRequest(string reason)
        {
            IntPtr reasonPtr = Marshal.StringToHGlobalAuto(reason);
            try
            {
                REASON_CONTEXT context = new REASON_CONTEXT()
                {
                    Version = POWER_REQUEST_CONTEXT_VERSION,
                    Flags = POWER_REQUEST_CONTEXT_FLAGS.POWER_REQUEST_CONTEXT_SIMPLE_STRING,
                    Reason = new REASON_CONTEXT.REASON_CONTEXT_UNION { SimpleReasonString = reasonPtr }

                };
                SafePowerHandle safePowerHandle = PowerCreateRequest(context);
                if (safePowerHandle.IsInvalid) { throw new Win32Exception(); }
                return safePowerHandle;
            }
            finally
            {
                if (reasonPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(reasonPtr);
                }
            }
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct REASON_CONTEXT
        {
            public uint Version;

            public POWER_REQUEST_CONTEXT_FLAGS Flags;

            public REASON_CONTEXT_UNION Reason;

            [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
            public struct REASON_CONTEXT_UNION
            {
                [FieldOffset(0)]
                public nint SimpleReasonString;

                // The DETAILED structure is not (yet) used, but needed for ARM CPUs, otherwise PowerCreateRequest fails, see #2745
                [FieldOffset(0)]
                public DETAILED Detailed;

                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
                public struct DETAILED
                {
                    public nint LocalizedReasonModule;
                    public uint LocalizedReasonId;
                    public uint ReasonStringCount;
                    public nint ReasonStrings;
                }
            }
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