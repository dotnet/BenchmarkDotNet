using System;
using System.Collections.Generic;
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

                return Marshal.PtrToStructure<Guid>(activeGuidPtr);
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
                    return "";
                res = PowerReadFriendlyName(IntPtr.Zero, activeGuidPtr, IntPtr.Zero, IntPtr.Zero, buffer, ref buffSize);
                if (res == ErrorMoreData)
                {
                    buffer.Capacity = (int)buffSize;
                    res = PowerReadFriendlyName(IntPtr.Zero, activeGuidPtr,
                        IntPtr.Zero, IntPtr.Zero, buffer, ref buffSize);
                }
                if (res != SuccessCode)
                    return "";

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

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, uint AccessFlags, uint Index, ref Guid Buffer, ref uint BufferSize);

        internal static IEnumerable<Guid> EnumerateAllPlanGuids()
        {
            const uint ACCESS_SCHEME = 16;
            uint index = 0;
            while (true)
            {
                Guid schemeGuid = Guid.Empty;
                uint size = (uint)Marshal.SizeOf(typeof(Guid));
                uint res = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ACCESS_SCHEME, index, ref schemeGuid, ref size);
                if (res != 0)
                    break;
                yield return schemeGuid;
                index++;
            }
        }

        internal static bool PlanExists(Guid planGuid)
        {
            foreach (var guid in EnumerateAllPlanGuids())
            {
                if (guid == planGuid)
                    return true;
            }
            return false;
        }
    }
}