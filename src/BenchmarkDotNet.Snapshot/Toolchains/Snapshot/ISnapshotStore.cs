using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    /// <summary>
    ///
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// Name of Snapshot Store
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Target of store
        /// </summary>
        string Filename { get; }

        /// <summary>
        /// Ger benchmark reuslt from store
        /// </summary>
        /// <param name="executeParameters"></param>
        /// <returns></returns>
        public ExecuteResult? GetResult(ExecuteParameters executeParameters);

        /// <summary>
        /// Export Begin
        /// </summary>
        /// <param name="logger">The logger</param>
        void ExportBegin(ILogger logger);

        /// <summary>
        /// Export End
        /// </summary>
        /// <param name="logger">>The logger</param>
        void ExportEnd(ILogger logger);

        /// <summary>
        /// Export <see cref="Summary"/> to store
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="logger"></param>
        void Export(Summary summary, ILogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="benchmarkCase"></param>
        /// <param name="logger"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver);
    }
}
