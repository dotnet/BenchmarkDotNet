using BenchmarkDotNet.Environments;
using Perfolizer.Models;

namespace BenchmarkDotNet.Models;

internal class BdnEnvironment : EnvironmentInfo
{
    public Jit? Jit { get; set; }
    public long? Affinity { get; set; }
}