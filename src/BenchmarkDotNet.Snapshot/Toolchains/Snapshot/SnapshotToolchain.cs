using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    public class SnapshotToolchain : Toolchain,
        IEquatable<SnapshotToolchain>
    {
        private readonly ISnapshotStore _store;

        private SnapshotToolchain(string name, ISnapshotStore store, IExecutor executor) :
            base(name,
                SnapshotGenerator.Default,
                SnpashotBuilder.Default,
                executor)
        {
            _store = store;
        }

        public static IToolchain From(ISnapshotStore store) =>
            new SnapshotToolchain(store.Name, store, new SnapshotExecutor(store));

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            return _store.IsSupported(benchmarkCase, logger, resolver);
        }


        public bool Equals(SnapshotToolchain other)
        {
            throw new NotImplementedException();
        }
    }
}
