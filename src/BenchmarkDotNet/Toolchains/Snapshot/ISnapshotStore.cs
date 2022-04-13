using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    public interface ISnapshotStore
    {
        public string Name { get; }
        string Filename { get; }

        public ExecuteResult GetResult(ExecuteParameters executeParameters);
        void ExportBegin(ILogger logger);
        void ExportEnd(ILogger logger);
        void Export(Summary summary, ILogger logger);
        bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver);
    }
}
