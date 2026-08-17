using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BenchmarkDotNet.Helpers
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SupportedOSPlatform("windows6.0.6000")]
    internal class PowerManagementHelper
    {
        internal static unsafe Guid? CurrentPlan
        {
            get
            {
                WIN32_ERROR res = PInvoke.PowerGetActiveScheme(null, out var activePolicyGuidPtr);
                if (res != WIN32_ERROR.NO_ERROR)
                    return null;

                var activePolicyGuid = *activePolicyGuidPtr;

                PInvoke.LocalFree((HLOCAL)activePolicyGuidPtr);

                return activePolicyGuid;
            }
        }

        internal unsafe static string CurrentPlanFriendlyName
        {
            get
            {
                WIN32_ERROR res = PInvoke.PowerGetActiveScheme(null, out var activeGuidPtr);
                if (res != WIN32_ERROR.NO_ERROR)
                    return "";

                Guid activeSchemaGuid = *activeGuidPtr;
                PInvoke.LocalFree((HLOCAL)activeGuidPtr);

                Span<byte> buffer = stackalloc byte[260];
                uint bufferSize = (uint)buffer.Length;

                res = PInvoke.PowerReadFriendlyName(null, activeSchemaGuid, null, null, buffer, ref bufferSize);
                if (res == WIN32_ERROR.ERROR_MORE_DATA)
                {
                    buffer = new byte[(int)bufferSize];
                    res = PInvoke.PowerReadFriendlyName(null, activeSchemaGuid, null, null, buffer, ref bufferSize);
                    bufferSize = (uint)buffer.Length;
                }

                if (res != WIN32_ERROR.NO_ERROR)
                    return "";

                ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(buffer.Slice(0, (int)bufferSize));
                return chars.TrimEnd('\0').ToString(); // Trim null terminator of PWSTR.
            }
        }

        internal static bool Set(Guid newPolicy)
        {
            return PInvoke.PowerSetActiveScheme(null, newPolicy) == WIN32_ERROR.NO_ERROR;
        }
    }
}
