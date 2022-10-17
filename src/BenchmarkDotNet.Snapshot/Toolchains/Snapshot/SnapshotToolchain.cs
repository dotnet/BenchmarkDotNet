using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    /// <summary>
    /// Build a benchmark from benchmark snapshot
    /// </summary>
    public class SnapshotToolchain : Toolchain,
        IEquatable<SnapshotToolchain>
    {
        private readonly ISnapshotStore _store;

        private SnapshotToolchain(string name, ISnapshotStore store, IExecutor executor) :
            base(name,
                SnapshotGenerator.Default,
                SnapshotBuilder.Default,
                executor)
        {
            _store = store;
        }

        /// <summary>
        /// Instance new <see cref="SnapshotToolchain"/>  from <see cref="ISnapshotStore"/>
        /// </summary>
        /// <param name="store">The store with contains the benchmark.</param>
        /// <returns></returns>
        public static IToolchain From(ISnapshotStore store) =>
            new SnapshotToolchain(store.Name, store, new SnapshotExecutor(store));

        /// <summary>
        /// Check is benchmark case is supported.
        /// </summary>
        /// <param name="benchmarkCase">The benchmark to validate</param>
        /// <param name="logger">The logger instance</param>
        /// <param name="resolver">The resolver</param>
        /// <returns>Return true if benchmark case is valid.</returns>
        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            return _store.IsSupported(benchmarkCase, logger, resolver);
        }


        /// <summary>
        /// NotImplementedException
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Equals(SnapshotToolchain? other)
        {
            throw new NotImplementedException();
        }
    }
}
