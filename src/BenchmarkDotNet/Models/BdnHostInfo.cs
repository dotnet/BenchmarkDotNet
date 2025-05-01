using Perfolizer.Models;

namespace BenchmarkDotNet.Models;

internal class BdnHostInfo : HostInfo
{
    public string RuntimeVersion { get; set; } = "";
    public bool HasAttachedDebugger { get; set; }
    public bool HasRyuJit { get; set; }
    public string Configuration { get; set; } = "";
    public string DotNetSdkVersion { get; set; } = "";
    public double ChronometerFrequency { get; set; }
    public string HardwareTimerKind { get; set; } = "";
}