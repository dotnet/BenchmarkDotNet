using JetBrains.Annotations;
using Perfolizer.Phd;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Phd;

[PublicAPI]
public class BdnHost : PhdHost
{
    public string RuntimeVersion { get; set; } = "";
    public bool HasAttachedDebugger { get; set; }
    public bool HasRyuJit { get; set; }
    public string Configuration { get; set; } = "";
    public string DotNetSdkVersion { get; set; } = "";
    public double ChronometerFrequency { get; set; }
    public string HardwareTimerKind { get; set; } = "";
}