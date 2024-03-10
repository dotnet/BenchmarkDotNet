using JetBrains.Annotations;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Phd;

[PublicAPI]
public class BdnBenchmark : PhdBenchmark
{
    public string DisplayInfo { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Type { get; set; } = "";
    public string Method { get; set; } = "";
    public string MethodTitle { get; set; } = "";
    public string Parameters { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? HardwareIntrinsics { get; set; } = "";
}