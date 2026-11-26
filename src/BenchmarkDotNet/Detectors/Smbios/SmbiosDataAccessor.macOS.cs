using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Detectors.Smbios;

internal static partial class SmbiosDataAccessor
{
    private static class MacOSDataAccessor
    {
        // Provided by the `AppleSMBIOS` I/O Registry service (x86_64 only).
        // `SMBIOS-EPS` is the SMBIOS entry point structure, `SMBIOS` is the raw DMI table.
        private const string IOKit = "/System/Library/Frameworks/IOKit.framework/IOKit";
        private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private const uint kCFStringEncodingASCII = 0x0600;

        public static bool TryGetSmbiosData(
            [NotNullWhen(true)] out Version? smbiosVersion,
            out ReadOnlySpan<byte> smbiosData)
        {
            smbiosVersion = null;
            smbiosData = [];

            // SMBIOS is not available on arm64.
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
                return false;

            IntPtr service = IOServiceGetMatchingService(0, IOServiceMatching("AppleSMBIOS"));
            if (service == IntPtr.Zero)
                return false;

            try
            {
                // Read the entry point to get the SMBIOS version.
                if (!TryReadProperty(service, "SMBIOS-EPS", out byte[] entryPoint))
                    return false;

                if (!TryParseVersion(entryPoint, out Version? version))
                    return false;

                // Read the raw DMI table.
                if (!TryReadProperty(service, "SMBIOS", out byte[] table))
                    return false;

                smbiosVersion = version;
                smbiosData = table;
                return true;
            }
            finally
            {
                IOObjectRelease(service);
            }
        }

        private static bool TryParseVersion(ReadOnlySpan<byte> entryPoint, [NotNullWhen(true)] out Version? version)
        {
            version = null;

            int versionOffset;
            if (entryPoint.StartsWith("_SM3_"u8))
                versionOffset = 0x07; // SMBIOS 3.0+ 64-bit entry point.
            else if (entryPoint.StartsWith("_SM_"u8))
                versionOffset = 0x06; // SMBIOS 2.x 32-bit entry point.
            else
                return false;

            if (entryPoint.Length < versionOffset + 2)
                return false;

            version = new Version(entryPoint[versionOffset], entryPoint[versionOffset + 1]);
            return true;
        }

        private static bool TryReadProperty(IntPtr service, string name, out byte[] bytes)
        {
            bytes = [];

            IntPtr key = CFStringCreateWithCString(IntPtr.Zero, name, kCFStringEncodingASCII);
            if (key == IntPtr.Zero)
                return false;

            try
            {
                IntPtr value = IORegistryEntryCreateCFProperty(service, key, IntPtr.Zero, 0);
                if (value == IntPtr.Zero)
                    return false;

                try
                {
                    int length = (int) CFDataGetLength(value);
                    if (length <= 0)
                        return false;

                    IntPtr ptr = CFDataGetBytePtr(value);
                    if (ptr == IntPtr.Zero)
                        return false;

                    bytes = new byte[length];
                    Marshal.Copy(ptr, bytes, 0, length);
                    return true;
                }
                finally
                {
                    CFRelease(value);
                }
            }
            finally
            {
                CFRelease(key);
            }
        }

        [DllImport(IOKit)]
        private static extern IntPtr IOServiceMatching([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(IOKit)]
        private static extern IntPtr IOServiceGetMatchingService(uint masterPort, IntPtr matching);

        [DllImport(IOKit)]
        private static extern IntPtr IORegistryEntryCreateCFProperty(IntPtr entry, IntPtr key, IntPtr allocator, uint options);

        [DllImport(IOKit)]
        private static extern int IOObjectRelease(IntPtr object_);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPStr)] string cString, uint encoding);

        [DllImport(CoreFoundation)]
        private static extern nint CFDataGetLength(IntPtr theData);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFDataGetBytePtr(IntPtr theData);

        [DllImport(CoreFoundation)]
        private static extern void CFRelease(IntPtr cf);
    }
}
