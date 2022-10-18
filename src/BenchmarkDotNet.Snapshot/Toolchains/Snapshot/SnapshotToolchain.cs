using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
        /// Validate benchmark case
        /// </summary>
        /// <param name="benchmarkCase">The benchmark to validate</param>
        /// <param name="resolver">The resolver</param>
        /// <returns></returns>
        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (_store.IsSupported(benchmarkCase, resolver))
            {
                yield return new ValidationError(false,
                    $"This benchmark '{benchmarkCase.DisplayInfo}' do not have a Snapshot, it will not be executed",
                    benchmarkCase);
            }
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
