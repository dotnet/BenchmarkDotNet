namespace BenchmarkDotNet.Detectors.Smbios;

internal interface ISmbiosStructureInfo
{
    SmbiosType Type { get; }
}
