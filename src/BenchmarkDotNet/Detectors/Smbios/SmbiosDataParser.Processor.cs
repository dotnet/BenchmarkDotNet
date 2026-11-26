namespace BenchmarkDotNet.Detectors.Smbios;

internal partial class SmbiosDataParser
{
    public static IReadOnlyList<SmbiosProcessorInfo> ParseProcessorInfoTable(ReadOnlySpan<byte> data)
    {
        var results = new List<SmbiosProcessorInfo>();
        var items = EnumerateStructures(data).Where(x => x.Type == SmbiosType.ProcessorInformation);
        foreach (SmbiosStructure item in items)
        {
            // Gets structure's formatted area (fixed-length fields).
            var bytes = data.Slice(item.Offset, item.Length);

            // Skip processor info if the socket is not populated.
            if (!SmbiosProcessorInfo.IsPopulated(bytes))
                continue;

            // Read the structure's string-set (unformatted area).
            var strings = ReadStringSet(data, item.StringSetStart, item.NextOffset);

            results.Add(new SmbiosProcessorInfo(bytes, strings));
        }

        return results;
    }
}
