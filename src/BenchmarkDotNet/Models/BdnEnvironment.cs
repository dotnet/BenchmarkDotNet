using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Perfolizer.Models;

namespace BenchmarkDotNet.Models;

internal class BdnEnvironment : EnvironmentInfo
{
    public RuntimeMoniker? Runtime { get; set; }
    public Jit? Jit { get; set; }
    public int? Affinity { get; set; }
}