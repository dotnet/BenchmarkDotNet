namespace BenchmarkDotNet.Tests.Detectors.Smbios;

/// <summary>
/// Entry point for building synthetic SMBIOS structures.
/// The low-level serialization helpers shared by the builders live in <see cref="SmbiosTableBuilder"/>.
/// </summary>
internal static partial class SmbiosTables
{
    /// <summary>
    /// Returns a copy of <paramref name="table"/> whose formatted area is truncated to
    /// <paramref name="newLength"/> bytes, keeping the string-set intact.
    /// Used to emulate structures from older SMBIOS versions.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="newLength"/> is greater than the current formatted area length.
    /// </exception>
    internal static byte[] TruncateFormattedArea(byte[] table, byte newLength)
    {
        const int lengthOffset = 0x01;

        byte currentLength = table[lengthOffset];
        if (newLength > currentLength)
            throw new ArgumentOutOfRangeException(nameof(newLength), "The new length must not exceed the current length.");

        int stringSetLength = table.Length - currentLength;
        byte[] result = new byte[newLength + stringSetLength];

        Array.Copy(table, 0, result, 0, newLength); // Formatted area
        Array.Copy(table, currentLength, result, newLength, stringSetLength); // String-set
        result[lengthOffset] = newLength;

        return result;
    }
}
