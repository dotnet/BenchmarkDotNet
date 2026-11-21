using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemInformation;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// Reads and walks the buffer returned by GetLogicalProcessorInformationEx
/// (an array of variable sized SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX entries).
/// </summary>
internal static unsafe class LogicalProcessorInformation
{
    // SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX { LOGICAL_PROCESSOR_RELATIONSHIP Relationship; DWORD Size; union { ... } }
    internal const int HeaderSize = sizeof(int) + sizeof(uint);

    internal delegate void EntryVisitor(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* entry);

    /// <summary>
    /// Returns the raw buffer for the given relationship, or <c>null</c> when it can't be retrieved.
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    internal static byte[]? Read(LOGICAL_PROCESSOR_RELATIONSHIP relationship)
    {
        // The size query is expected to fail with ERROR_INSUFFICIENT_BUFFER and report the required length.
        uint bufferLength = 0;
        PInvoke.GetLogicalProcessorInformationEx(relationship, null, ref bufferLength);
        var error = (WIN32_ERROR) Marshal.GetLastWin32Error();
        if (bufferLength == 0 || error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            return null;

        var buffer = new byte[bufferLength];
        fixed (byte* bufferPtr = buffer)
        {
            if (!PInvoke.GetLogicalProcessorInformationEx(relationship, (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*) bufferPtr, &bufferLength))
                return null;
        }

        if (bufferLength < buffer.Length)
            Array.Resize(ref buffer, (int) bufferLength);

        return buffer;
    }

    /// <summary>
    /// Calls <paramref name="visitor"/> for every complete entry of the buffer and stops at the first malformed one.
    /// Every visited entry has at least <see cref="HeaderSize"/> bytes and fits into the buffer, with its <c>Size</c> bytes.
    /// </summary>
    internal static void ForEachEntry(byte[] buffer, EntryVisitor visitor)
    {
        fixed (byte* bufferPtr = buffer)
        {
            byte* currentPtr = bufferPtr;
            byte* endPtr = bufferPtr + buffer.Length;
            while (endPtr - currentPtr >= HeaderSize)
            {
                var entry = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*) currentPtr;
                uint size = entry->Size;
                if (size < HeaderSize || size > endPtr - currentPtr)
                    break;

                visitor(entry);

                currentPtr += size;
            }
        }
    }
}
