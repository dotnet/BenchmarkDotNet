using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Portability
{
    internal interface IBenchmarkConverter
    {
        Benchmark[] UrlToBenchmarks(string url, IConfig config = null);
        Benchmark[] SourceToBenchmarks(string source, IConfig config = null);
    }
}