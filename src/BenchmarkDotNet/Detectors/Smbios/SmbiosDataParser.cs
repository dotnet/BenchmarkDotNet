using System.Text;

namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Parser for raw SMBIOS data obtained via GetSystemFirmwareTable(RSMB).
/// Definitions are based on DSP0134 (SMBIOS Reference Specification) 3.10.0
/// </summary>
internal partial class SmbiosDataParser
{
    // Represents a single SMBIOS structure.
    private readonly record struct SmbiosStructure(
        SmbiosType Type,
        int Offset,
        byte Length,
        int StringSetStart,
        int NextOffset
    );

    private static IReadOnlyList<SmbiosStructure> EnumerateStructures(ReadOnlySpan<byte> data)
    {
        var results = new List<SmbiosStructure>();
        int offset = 0;

        while (offset + 4 <= data.Length)
        {
            SmbiosType type = (SmbiosType) data[offset]; // 0x00: type
            if (type == SmbiosType.EndOfTable)
                break;

            // Length includes the 4-byte header (type/length/handle)
            byte length = data[offset + 1]; // 0x01: length

            // Check returned length.
            int stringSetStart = offset + length;
            if (length < 4 || stringSetStart > data.Length)
                break;

            // Find next structure offset.
            if (!TryFindNextStructureOffset(data, stringSetStart, out int nextOffset))
                break;

            results.Add(new SmbiosStructure(type, offset, length, stringSetStart, nextOffset));
            offset = nextOffset;
        }

        return results;
    }

    private static bool TryFindNextStructureOffset(ReadOnlySpan<byte> data, int stringSetStart, out int nextOffset)
    {
        // String-set is a sequence of null-terminated strings, terminated by an extra null (double-null).
        // Scan for 0x00 0x00. If there is no string-set content, the structure is immediately followed by 0x00 0x00.
        for (int i = stringSetStart; i + 1 < data.Length; i++)
        {
            if (data[i] == 0 && data[i + 1] == 0)
            {
                nextOffset = i + 2;
                return true;
            }
        }

        nextOffset = -1;
        return false;
    }

    private static IReadOnlyList<string> ReadStringSet(ReadOnlySpan<byte> data, int start, int end)
    {
        // Exclude the double-null terminator.
        if (end - start <= 2)
            return [];

        ReadOnlySpan<byte> stringSet = data.Slice(start, end - start - 2);
        List<string> strings = new();
        int stringStart = 0;
        for (int i = 0; i < stringSet.Length; i++)
        {
            if (stringSet[i] != 0)
                continue;

            strings.Add(DecodeString(stringSet.Slice(stringStart, i - stringStart)));
            stringStart = i + 1;
        }

        // Add the last string, which is not followed by a separator.
        if (stringStart < stringSet.Length)
            strings.Add(DecodeString(stringSet.Slice(stringStart)));

        return strings;
    }

    private static string DecodeString(ReadOnlySpan<byte> data)
    {
#if NET
        return Encoding.UTF8.GetString(data);
#else
        return Encoding.UTF8.GetString(data.ToArray());
#endif
    }
}
