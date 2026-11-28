using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace BenchmarkDotNet.Detectors.Smbios;

internal static partial class SmbiosDataAccessor
{
    [SupportedOSPlatform("windows6.0.6000")]
    private static class WindowsDataAccessor
    {
        private const FIRMWARE_TABLE_PROVIDER RSMB = FIRMWARE_TABLE_PROVIDER.RSMB; // 0x52534D42;

        private const int RsmbHeaderSize = 8;

        public static bool TryGetSmbiosData(
            [NotNullWhen(true)] out Version? smbiosVersion,
            out ReadOnlySpan<byte> smbiosData)
        {
            smbiosVersion = null;
            smbiosData = [];

            ReadOnlySpan<byte> buffer = GetRawSmbiosData();
            if (buffer.Length < RsmbHeaderSize)
                return false;

            // Parse data from RawSMBIOSData Format: https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemfirmwaretable
            byte majorVersion = buffer[0x01];
            byte minorVersion = buffer[0x02];
            int tableLength = (int) Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(0x04)), (uint) (buffer.Length - RsmbHeaderSize));

            smbiosVersion = new Version(majorVersion, minorVersion);
            smbiosData = buffer.Slice(RsmbHeaderSize, tableLength);
            return true;
        }

        private static ReadOnlySpan<byte> GetRawSmbiosData()
        {
            // The table can grow between the queries, so retry a few times.
            const int MaxRetries = 5;

            // Initial buffer size. SMBIOS data is usually less than 4KB.
            uint requiredSize = 4096;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    // Query size of SMBIOS data.
                    requiredSize = PInvoke.GetSystemFirmwareTable(RSMB, 0, []);
                    if (requiredSize < RsmbHeaderSize)
                        return [];
                }

                // Gets SMBIOS data.
                byte[] buffer = new byte[requiredSize];
                uint written = PInvoke.GetSystemFirmwareTable(RSMB, 0, buffer);

                if (written > requiredSize)
                    continue; // Retry when buffer size is not sufficient.

                if (written == 0 || written < RsmbHeaderSize)
                    return [];

                if (written == requiredSize)
                    return buffer;

                return buffer.AsSpan(0, (int) written);
            }

            return [];
        }
    }
}
