using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace BenchmarkDotNet.Helpers
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class PowerManagementHelper
    {
        private const uint ErrorMoreData = 234;
        private const uint SuccessCode = 0;

        internal static Guid? CurrentPlan
        {
            get
            {
                IntPtr activeGuidPtr = IntPtr.Zero;
                uint res = PowerGetActiveScheme(IntPtr.Zero, ref activeGuidPtr);
                if (res != SuccessCode)
                    return null;

                return (Guid)Marshal.PtrToStructure(activeGuidPtr, typeof(Guid));
            }
        }

        internal static string CurrentPlanFriendlyName
        {
            get
            {
                uint buffSize = 0;
                StringBuilder buffer = new StringBuilder();
                IntPtr activeGuidPtr = IntPtr.Zero;
                uint res = PowerGetActiveScheme(IntPtr.Zero, ref activeGuidPtr);
                if (res != SuccessCode)
                    return null;
                res = PowerReadFriendlyName(IntPtr.Zero, activeGuidPtr, IntPtr.Zero, IntPtr.Zero, buffer, ref buffSize);
                if (res == ErrorMoreData)
                {
                    buffer.Capacity = (int)buffSize;
                    res = PowerReadFriendlyName(IntPtr.Zero, activeGuidPtr,
                        IntPtr.Zero, IntPtr.Zero, buffer, ref buffSize);
                }
                if (res != SuccessCode)
                    return null;

                return buffer.ToString();
            }
        }

        internal static bool Set(Guid newPolicy)
        {
           return PowerSetActiveScheme(IntPtr.Zero, ref newPolicy) == 0;
        }

        [DllImport("powrprof.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, IntPtr PowerSettingGuid, StringBuilder Buffer, ref uint BufferSize);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        private static extern int PowerSetActiveScheme(IntPtr ReservedZero, ref Guid policyGuid);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, ref IntPtr ActivePolicyGuid);
    }
}
