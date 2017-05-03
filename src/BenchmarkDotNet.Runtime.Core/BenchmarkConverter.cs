using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet
{
    internal class BenchmarkConverter : IBenchmarkConverter
    {
        public Benchmark[] UrlToBenchmarks(string url, IConfig config = null) 
            => throw new System.NotSupportedException("Running benchmark from url is not supported for .NET Core/UWP");

        public Benchmark[] SourceToBenchmarks(string source, IConfig config = null) 
            => throw new System.NotSupportedException("Running benchmark from source is not supported for .NET Core/UWP");
    }
}