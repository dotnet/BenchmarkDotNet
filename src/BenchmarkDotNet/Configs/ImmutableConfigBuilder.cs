using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Configs
{
    /// <summary>
    /// this class is responsible for config that has no duplicates, does all of the internal hacks and is ready to run
    /// </summary>
    public static class ImmutableConfigBuilder
    {
        private static readonly IValidator[] MandatoryValidators =
        {
            BaselineValidator.FailOnError,
            SetupCleanupValidator.FailOnError,
            AwaitableAsyncEnumerableAmbiguityValidator.DontFailOnError,
            RunModeValidator.FailOnError,
            DiagnosersValidator.Composite,
            CompilationValidator.FailOnError,
            ConfigValidator.DontFailOnError,
            ShadowCopyValidator.DontFailOnError,
            JitOptimizationsValidator.DontFailOnError,
            DeferredExecutionValidator.DontFailOnError,
            ParamsAllValuesValidator.FailOnError,
            ParamsValidator.FailOnError,
            BenchmarkCancellationValidator.FailOnError
        };

        /// <summary>
        /// removes duplicates and applies all extra logic required to make the config a final one
        /// </summary>
        public static ImmutableConfig Create(IConfig source)
        {
            var uniqueColumnProviders = source.GetColumnProviders().Distinct().ToImmutableArray();
            var uniqueLoggers = source.GetLoggers().ToImmutableHashSet();
            var configAnalyse = new List<Conclusion>();

            var uniqueHardwareCounters = source.GetHardwareCounters().Where(counter => counter != HardwareCounter.NotSet).ToImmutableHashSet();
            var uniqueDiagnosers = GetDiagnosers(source.GetDiagnosers(), uniqueHardwareCounters);
            var uniqueExporters = GetExporters(source.GetExporters(), uniqueDiagnosers, configAnalyse);
            var uniqueAnalyzers = GetAnalysers(source.GetAnalysers(), uniqueDiagnosers);

            var uniqueValidators = GetValidators(source.GetValidators(), MandatoryValidators, source.Options);

            var uniqueFilters = source.GetFilters().ToImmutableHashSet();
            var uniqueRules = source.GetLogicalGroupRules().ToImmutableArray();
            var uniqueHidingRules = source.GetColumnHidingRules().ToImmutableArray();

            var uniqueRunnableJobs = GetRunnableJobs(source.GetJobs()).ToImmutableHashSet();
            var uniqueEventProcessors = source.GetEventProcessors().ToImmutableHashSet();

            return new ImmutableConfig(
                uniqueColumnProviders,
                uniqueLoggers,
                uniqueHardwareCounters,
                uniqueDiagnosers,
                uniqueExporters,
                uniqueAnalyzers,
                uniqueValidators,
                uniqueFilters,
                uniqueRules,
                uniqueHidingRules,
                uniqueRunnableJobs,
                uniqueEventProcessors,
                source.UnionRule,
                source.ArtifactsPath ?? DefaultConfig.Instance.ArtifactsPath!,
                source.Title,
                source.CultureInfo ?? DefaultCultureInfo.Instance,
                source.Orderer ?? DefaultOrderer.Instance,
                source.CategoryDiscoverer ?? DefaultCategoryDiscoverer.Instance,
                source.HardwareCounterProfile ?? DefaultHardwareCounterProfile.Instance,
                source.SummaryStyle ?? SummaryStyle.Default,
                source.Options,
                source.BuildTimeout,
                source.WakeLock,
                configAnalyse.AsReadOnly()
            );
        }

        private static ImmutableHashSet<IDiagnoser> GetDiagnosers(IEnumerable<IDiagnoser> diagnosers, ImmutableHashSet<HardwareCounter> uniqueHardwareCounters)
        {
            var builder = ImmutableHashSet.CreateBuilder(new TypeComparer<IDiagnoser>());

            foreach (var diagnoser in diagnosers)
                if (!builder.Contains(diagnoser))
                    builder.Add(diagnoser);

            if (!uniqueHardwareCounters.IsEmpty && !diagnosers.OfType<IHardwareCountersDiagnoser>().Any())
            {
                // if users define hardware counters via [HardwareCounters] we need to dynamically load the right diagnoser
                var hardwareCountersDiagnoser = DiagnosersLoader.GetImplementation<IHardwareCountersDiagnoser>();

                if (hardwareCountersDiagnoser != default(IDiagnoser) && !builder.Contains(hardwareCountersDiagnoser))
                    builder.Add(hardwareCountersDiagnoser);
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<IExporter> GetExporters(IEnumerable<IExporter> exporters,
            ImmutableHashSet<IDiagnoser> uniqueDiagnosers,
            IList<Conclusion> configAnalyse)
        {

            void AddWarning(string message)
            {
                var conclusion = Conclusion.CreateWarning("Configuration", message);
                configAnalyse.Add(conclusion);
            }

            var mergeDictionary = new Dictionary<string, IExporter>();

            foreach (var exporter in exporters)
            {
                var exporterName = exporter.Name;
                if (mergeDictionary.ContainsKey(exporterName))
                {
                    AddWarning($"The exporter {exporterName} is already present in configuration. There may be unexpected results.");
                }
                mergeDictionary[exporterName] = exporter;
            }


            foreach (var diagnoser in uniqueDiagnosers)
                foreach (var exporter in diagnoser.Exporters)
                {
                    var exporterName = exporter.Name;
                    if (mergeDictionary.ContainsKey(exporterName))
                    {
                        AddWarning($"The exporter {exporterName} of {diagnoser.GetType().Name} is already present in configuration. There may be unexpected results.");
                    }
                    mergeDictionary[exporterName] = exporter;
                }

            var result = mergeDictionary.Values.ToList();


            var hardwareCounterDiagnoser = uniqueDiagnosers.OfType<IHardwareCountersDiagnoser>().SingleOrDefault();
            var disassemblyDiagnoser = uniqueDiagnosers.OfType<DisassemblyDiagnoser>().SingleOrDefault();

            // we can use InstructionPointerExporter only when we have both IHardwareCountersDiagnoser and DisassemblyDiagnoser
            if (hardwareCounterDiagnoser != default(IHardwareCountersDiagnoser) && disassemblyDiagnoser != default(DisassemblyDiagnoser))
                result.Add(new InstructionPointerExporter(hardwareCounterDiagnoser, disassemblyDiagnoser));

            for (int i = result.Count - 1; i >= 0; i--)
                if (result[i] is IExporterDependencies exporterDependencies)
                    foreach (var dependency in exporterDependencies.Dependencies)
                        /*
                         *  When exporter that depends on an other already present in the configuration print warning.
                         *
                         *  Example:
                         *
                         *  // Global Current Culture separator is Semicolon;
                         *  [CsvMeasurementsExporter(CsvSeparator.Comma)] // force use Comma
                         *  [RPlotExporter]
                         *  public class MyBanch
                         *  {
                         *
                         *  }
                         *
                         *  RPlotExporter is depend from CsvMeasurementsExporter.Default
                         *
                         *  On active logger will by print:
                         *  "The CsvMeasurementsExporter is already present in the configuration. There may be unexpected results of RPlotExporter.
                         *
                         */
                        if (!result.Any(exporter => exporter.GetType() == dependency.GetType()))
                            result.Insert(i, dependency); // All the exporter dependencies should be added before the exporter
                        else
                        {
                            AddWarning($"The {dependency.Name} is already present in the configuration. There may be unexpected results of {result[i].GetType().Name}.");
                        }

            result.Sort((left, right) => (left is IExporterDependencies).CompareTo(right is IExporterDependencies)); // the case when they were defined by user in wrong order ;)

            return result.ToImmutableArray();
        }

        private static ImmutableHashSet<IAnalyser> GetAnalysers(IEnumerable<IAnalyser> analysers, ImmutableHashSet<IDiagnoser> uniqueDiagnosers)
        {
            var builder = ImmutableHashSet.CreateBuilder<IAnalyser>();

            foreach (var analyser in analysers)
                if (!builder.Contains(analyser))
                    builder.Add(analyser);

            foreach (var diagnoser in uniqueDiagnosers)
                foreach (var analyser in diagnoser.Analysers)
                    if (!builder.Contains(analyser))
                        builder.Add(analyser);

            return builder.ToImmutable();
        }

        private static ImmutableHashSet<IValidator> GetValidators(IEnumerable<IValidator> configuredValidators, IValidator[] mandatoryValidators, ConfigOptions options)
        {
            var builder = ImmutableHashSet.CreateBuilder<IValidator>();

            foreach (var validator in configuredValidators
                .Concat(mandatoryValidators)
                .GroupBy(validator => validator.GetType())
                .Select(groupedByType => groupedByType.FirstOrDefault(validator => validator.TreatsWarningsAsErrors) ?? groupedByType.First()))
            {
                builder.Add(validator);
            }

            if (options.IsSet(ConfigOptions.DisableOptimizationsValidator) && builder.Contains(JitOptimizationsValidator.DontFailOnError))
                builder.Remove(JitOptimizationsValidator.DontFailOnError);
            if (options.IsSet(ConfigOptions.DisableOptimizationsValidator) && builder.Contains(JitOptimizationsValidator.FailOnError))
                builder.Remove(JitOptimizationsValidator.FailOnError);

            return builder.ToImmutable();
        }

        /// <summary>
        /// returns a set of unique jobs that are ready to run
        /// </summary>
        private static IReadOnlyList<Job> GetRunnableJobs(IEnumerable<Job> jobs)
        {
            var unique = jobs.Distinct(JobComparer.Default).ToArray();
            var result = new List<Job>();

            foreach (var standardJob in unique.Where(job => !job.Meta.IsMutator && !job.Meta.IsDefault))
                result.Add(standardJob);

            var customDefaultJob = unique.SingleOrDefault(job => job.Meta.IsDefault);
            var defaultJob = customDefaultJob ?? Job.Default;

            if (!result.Any())
                result.Add(defaultJob);

            foreach (var mutatorJob in unique.Where(job => job.Meta.IsMutator))
            {
                for (int i = 0; i < result.Count; i++)
                {
                    var copy = result[i].UnfreezeCopy();

                    copy.Apply(mutatorJob);
                    ApplyCouplingFromMutator(copy, mutatorJob);

                    result[i] = copy.Freeze();
                }
            }

            // Deduplicated again: the pass at the top of this method runs before the mutators are merged, so it
            // cannot see jobs that only become identical here.
            return result
                .Select(ReconcileRuntimeAndToolchain)
                .Distinct(JobComparer.Default)
                .ToArray();
        }

        /// <summary>
        /// Re-applies the Runtime/Toolchain coupling in the direction the mutator asked for.
        /// </summary>
        /// <remarks>
        /// <c>Apply</c> writes characteristics directly, past the <see cref="InfrastructureMode" /> setters that keep
        /// the pair in sync, so a merged job can end up with the two disagreeing. The mutator is the later and more
        /// specific instruction, so whichever of them it set wins - as it would through the setters.
        /// </remarks>
        private static void ApplyCouplingFromMutator(Job job, Job mutatorJob)
        {
            if (mutatorJob.Infrastructure.HasValue(InfrastructureMode.ToolchainCharacteristic))
                InfrastructureMode.RuntimeCharacteristic[job.Infrastructure] = job.Infrastructure.Toolchain?.Runtime;
            else if (mutatorJob.Infrastructure.HasValue(InfrastructureMode.RuntimeCharacteristic))
                InfrastructureMode.ToolchainCharacteristic[job.Infrastructure] = null;
        }

        /// <summary>
        /// Keeps a job's Runtime in sync with an explicitly set Toolchain, where nothing says which was meant to win.
        /// </summary>
        /// <remarks>
        /// The public characteristic indexers write past the setters too. With no mutator to say which assignment came
        /// later, the toolchain wins: it is what builds and runs the benchmark, so a stale runtime would report one
        /// nothing ran on.
        /// </remarks>
        private static Job ReconcileRuntimeAndToolchain(Job job)
        {
            var toolchain = job.Infrastructure.Toolchain;
            if (toolchain == null || Equals(job.Infrastructure.Runtime, toolchain.Runtime))
                return job;

            var copy = job.UnfreezeCopy();
            InfrastructureMode.RuntimeCharacteristic[copy.Infrastructure] = toolchain.Runtime;
            return copy.Freeze();
        }

        private class TypeComparer<TInterface> : IEqualityComparer<TInterface>
        {
            // different types can implement the same interface, we want to distinct by type
            public bool Equals(TInterface? x, TInterface? y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x is null || y is null)
                    return false;

                return x.GetType() == y.GetType();
            }

            public int GetHashCode(TInterface obj) => obj?.GetType().GetHashCode() ?? 0;
        }
    }
}
