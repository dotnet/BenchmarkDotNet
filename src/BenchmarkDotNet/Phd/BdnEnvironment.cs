using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Phd;

public class BdnEnvironment : PhdEnvironment
{
    public RuntimeMoniker? Runtime { get; set; }
    public Jit? Jit { get; set; }
    public int? Affinity { get; set; }
}