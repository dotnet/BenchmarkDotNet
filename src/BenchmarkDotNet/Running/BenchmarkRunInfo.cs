using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Running
{
    /// <summary>
    /// One reading of one type: the cases it declares, and what BenchmarkDotNet learned about it while reading.
    /// Obtained from <see cref="BenchmarkConverter"/> or <see cref="TypeFilter"/> rather than constructed, because
    /// only reading a type can answer for what is in here. <see cref="WithBenchmarks"/> narrows one to some of its
    /// cases and <see cref="WithConfig"/> runs them under a different config; neither re-reads the type.
    /// </summary>
    public sealed class BenchmarkRunInfo : IDisposable, IAsyncDisposable
    {
        internal BenchmarkRunInfo(
            BenchmarkCase[] benchmarksCases,
            Type type,
            ImmutableConfig config,
            bool containsBenchmarkDeclarations,
            CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            ValidationError[] declarationErrors)
        {
            // A reading is of one type, and everything else it holds was learned from that type - a case belonging
            // to another would be described by a config, a diagnoser and a set of errors that never judged it.
            if (benchmarksCases.FirstOrDefault(benchmark => benchmark.Descriptor.Type != type) is { } foreign)
            {
                throw new ArgumentException(
                    $"{foreign.Descriptor.Type.GetDisplayName()}.{foreign.Descriptor.WorkloadMethod.Name} is not declared by"
                    + $" {type.GetDisplayName()}, so it is not one of its cases. Please, narrow the cases this reading"
                    + " already holds rather than bringing in another type's.",
                    nameof(benchmarksCases));
            }

            BenchmarksCases = benchmarksCases;
            Type = type;
            Config = config;
            ContainsBenchmarkDeclarations = containsBenchmarkDeclarations;
            CompositeInProcessDiagnoser = compositeInProcessDiagnoser;
            DeclarationErrors = declarationErrors;
        }

        /// <summary>
        /// The same reading of the same type, narrowed to some of its cases. Everything else the reading learned
        /// still holds - the type was read once, and leaving out some of its cases does not re-read it.
        /// </summary>
        public BenchmarkRunInfo WithBenchmarks(BenchmarkCase[] benchmarksCases)
            => new(benchmarksCases, Type, Config, benchmarksCases.Length > 0, CompositeInProcessDiagnoser, DeclarationErrors);

        /// <summary>
        /// The same cases of the same type, to be run under a different config.
        /// </summary>
        public BenchmarkRunInfo WithConfig(ImmutableConfig config)
            => new(BenchmarksCases, Type, config, ContainsBenchmarkDeclarations, CompositeInProcessDiagnoser, DeclarationErrors);

        public ValueTask DisposeAsync() => BenchmarksCases.DisposeAllAsync();

        public void Dispose()
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            context.ExecuteUntilComplete(DisposeAsync());
        }

        public BenchmarkCase[] BenchmarksCases { get; }
        public Type Type { get; }
        public ImmutableConfig Config { get; }
        public bool ContainsBenchmarkDeclarations { get; }
        public CompositeInProcessDiagnoser CompositeInProcessDiagnoser { get; }

        /// <summary>
        /// The declarations discovery could not build a <see cref="BenchmarkCase"/> from. Reading a type yields these
        /// instead of stopping at the first one, so a run reports every bad declaration at once and the members that
        /// could be read still run - which is also what lets <c>--list</c> and the test adapter enumerate a type that
        /// has one bad member. Each is critical, so a run that has any of them stops after the validation stage.
        /// </summary>
        public ValidationError[] DeclarationErrors { get; }
    }
}
