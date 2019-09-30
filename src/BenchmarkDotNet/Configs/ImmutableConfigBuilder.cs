﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Validators;

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
            RunModeValidator.FailOnError,
            DiagnosersValidator.Composite,
            CompilationValidator.FailOnError,
            ConfigValidator.DontFailOnError,
            ShadowCopyValidator.DontFailOnError,
            JitOptimizationsValidator.DontFailOnError,
            DeferredExecutionValidator.DontFailOnError
        };

        /// <summary>
        /// removes duplicates and applies all extra logic required to make the config a final one
        /// </summary>
        public static ImmutableConfig Create(IConfig source)
        {
            var uniqueColumnProviders = source.GetColumnProviders().Distinct().ToImmutableArray();
            var uniqueLoggers = source.GetLoggers().ToImmutableHashSet();

            var uniqueHardwareCounters = source.GetHardwareCounters().ToImmutableHashSet();
            var uniqueDiagnosers = GetDiagnosers(source.GetDiagnosers(), uniqueHardwareCounters);
            var uniqueExporters = GetExporters(source.GetExporters(), uniqueDiagnosers);
            var unqueAnalyzers = GetAnalysers(source.GetAnalysers(), uniqueDiagnosers);

            var uniqueValidators = GetValidators(source.GetValidators(), MandatoryValidators, source.Options);

            var uniqueFilters = source.GetFilters().ToImmutableHashSet();
            var uniqueRules = source.GetLogicalGroupRules().ToImmutableHashSet();

            var uniqueRunnableJobs = GetRunnableJobs(source.GetJobs()).ToImmutableHashSet();

            return new ImmutableConfig(
                uniqueColumnProviders,
                uniqueLoggers,
                uniqueHardwareCounters,
                uniqueDiagnosers,
                uniqueExporters,
                unqueAnalyzers,
                uniqueValidators,
                uniqueFilters,
                uniqueRules,
                uniqueRunnableJobs,
                source.UnionRule,
                source.ArtifactsPath ?? DefaultConfig.Instance.ArtifactsPath,
                source.Encoding,
                source.Orderer ?? DefaultOrderer.Instance,
                source.SummaryStyle,
                source.Options
            );
        }

        private static ImmutableHashSet<IDiagnoser> GetDiagnosers(IEnumerable<IDiagnoser> diagnosers, ImmutableHashSet<HardwareCounter> uniqueHardwareCoutners)
        {
            var builder = ImmutableHashSet.CreateBuilder(new TypeComparer<IDiagnoser>());

            foreach (var diagnoser in diagnosers)
                if (!builder.Contains(diagnoser))
                    builder.Add(diagnoser);

            if (!uniqueHardwareCoutners.IsEmpty && !diagnosers.OfType<IHardwareCountersDiagnoser>().Any())
            {
                // if users define hardware counters via [HardwareCounters] we need to dynamically load the right diagnoser
                var hardwareCountersDiagnoser = DiagnosersLoader.GetImplementation<IHardwareCountersDiagnoser>();

                if (hardwareCountersDiagnoser != default && !builder.Contains(hardwareCountersDiagnoser))
                    builder.Add(hardwareCountersDiagnoser);
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<IExporter> GetExporters(IEnumerable<IExporter> exporters, ImmutableHashSet<IDiagnoser> uniqueDiagnosers)
        {
            var result = new List<IExporter>();

            foreach (var exporter in exporters)
                if (!result.Contains(exporter))
                    result.Add(exporter);

            foreach (var diagnoser in uniqueDiagnosers)
            foreach (var exporter in diagnoser.Exporters)
                if (!result.Contains(exporter))
                    result.Add(exporter);

            var hardwareCounterDiagnoser = uniqueDiagnosers.OfType<IHardwareCountersDiagnoser>().SingleOrDefault();
            var disassemblyDiagnoser = uniqueDiagnosers.OfType<IDisassemblyDiagnoser>().SingleOrDefault();

            // we can use InstructionPointerExporter only when we have both IHardwareCountersDiagnoser and IDisassemblyDiagnoser
            if (hardwareCounterDiagnoser != default && disassemblyDiagnoser != default)
                result.Add(new InstructionPointerExporter(hardwareCounterDiagnoser, disassemblyDiagnoser));

            for (int i = result.Count - 1; i >=0; i--)
                if (result[i] is IExporterDependencies exporterDependencies)
                    foreach (var dependency in exporterDependencies.Dependencies)
                        if (!result.Contains(dependency))
                            result.Insert(i, dependency); // All the exporter dependencies should be added before the exporter

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
                if (builder.Contains(analyser))
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
            var unique = jobs.Distinct().ToArray();
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

                    result[i] = copy.Freeze();
                }
            }

            return result;
        }

        private class TypeComparer<TInterface> : IEqualityComparer<TInterface>
        {
            // different types can implement the same interface, we want to distinct by type
            public bool Equals(TInterface x, TInterface y) => x.GetType() == y.GetType();

            public int GetHashCode(TInterface obj) => obj.GetType().GetHashCode();
        }
    }
}