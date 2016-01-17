using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Plugins.Toolchains;
using System.Collections.Generic;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPlugins
    {
        IBenchmarkLogger CompositeLogger { get; }
        IBenchmarkExporter CompositeExporter { get; }
        IBenchmarkDiagnoser CompositeDiagnoser { get; }
        IBenchmarkAnalyser CompositeAnalyser { get; }
        IList<IBenchmarkResultExtender> ResultExtenders { get; }
        IBenchmarkToolchainFacade CreateToolchain(Benchmark benchmark, IBenchmarkLogger logger);
    }
}