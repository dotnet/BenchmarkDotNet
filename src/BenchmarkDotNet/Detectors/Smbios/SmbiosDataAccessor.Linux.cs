using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Detectors.Smbios;

internal static partial class SmbiosDataAccessor
{
    private static class LinuxDataAccessor
    {
        // Provided by the kernel's DMI sysfs interface (CONFIG_DMI_SYSFS).
        private const string EntryPointPath = "/sys/firmware/dmi/tables/smbios_entry_point";
        private const string TablePath = "/sys/firmware/dmi/tables/DMI";

        public static bool TryGetSmbiosData(
            [NotNullWhen(true)] out Version? smbiosVersion,
            out ReadOnlySpan<byte> smbiosData)
        {
            smbiosVersion = null;
            smbiosData = [];

            if (!TryReadAllBytes(EntryPointPath, out byte[] entryPoint))
                return false;

            if (!TryParseVersion(entryPoint, out Version? version))
                return false;

            if (!TryReadAllBytes(TablePath, out byte[] table))
                return false;

            smbiosVersion = version;
            smbiosData = table;
            return true;
        }

        private static bool TryReadAllBytes(string path, out byte[] bytes)
        {
            bytes = [];
            try
            {
                // sysfs reports a file length of 0, so read until EOF instead of relying on the size.
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                bytes = buffer.ToArray();
                return true;
            }
            catch (Exception)
            {
                // SMBIOS related files are usually mode 0400 (root only)
                // So it's expected to be failed if the process is not elevated.
                return false;
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
    }
}
