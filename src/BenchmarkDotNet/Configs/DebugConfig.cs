using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;

using JetBrains.Annotations;

namespace BenchmarkDotNet.Configs
{
    /// <summary>
    /// config which allows to debug benchmarks running it in the same process
    /// </summary>
    [PublicAPI]
    public class DebugInProcessConfig : DebugConfig
    {
        public override IEnumerable<Job> GetJobs()
            => new[]
            {
                Job.Default
                    .With(
                        new InProcessEmitToolchain(
                            TimeSpan.FromHours(1), // 1h should be enough to debug the benchmark
                            true))
            };
    }

    /// <summary>
    /// config which allows to build benchmarks in Debug
    /// </summary>
    [PublicAPI]
    public class DebugBuildConfig : DebugConfig
    {
        public override IEnumerable<Job> GetJobs()
            => new[]
            {
                Job.Default
                    .With(GetToolchainThatGeneratesProjectFile())
                    .WithCustomBuildConfiguration("Debug") // will do `-c Debug everywhere` 
            };

        private IToolchain GetToolchainThatGeneratesProjectFile()
        {
            switch (RuntimeInformation.GetCurrentRuntime())
            {
                case ClrRuntime _:
                case MonoRuntime _:
                    return CsProjClassicNetToolchain.Current.Value;
                case CoreRuntime _:
                    return CsProjCoreToolchain.Current.Value;
                case CoreRtRuntime _:
                    return CoreRtToolchain.LatestBuild;
                default:
                    throw new NotSupportedException("Runtime not supported!");
            }
        }
    }

    public abstract class DebugConfig : IConfig
    {
        public abstract IEnumerable<Job> GetJobs();

        public IEnumerable<IValidator> GetValidators() => Array.Empty<IValidator>();
        public IEnumerable<IColumnProvider> GetColumnProviders() => DefaultColumnProviders.Instance;
        public IEnumerable<IExporter> GetExporters() => Array.Empty<IExporter>();
        public IEnumerable<ILogger> GetLoggers() => new[] { ConsoleLogger.Default };
        public IEnumerable<IDiagnoser> GetDiagnosers() => Array.Empty<IDiagnoser>();
        public IEnumerable<IAnalyser> GetAnalysers() => Array.Empty<IAnalyser>();
        public IEnumerable<HardwareCounter> GetHardwareCounters() => Array.Empty<HardwareCounter>();
        public IEnumerable<IFilter> GetFilters() => Array.Empty<IFilter>();

        public IOrderer Orderer => DefaultOrderer.Instance;
        public SummaryStyle SummaryStyle => SummaryStyle.Default;
        public ConfigUnionRule UnionRule => ConfigUnionRule.Union;
        public string ArtifactsPath => Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts");
        public Encoding Encoding => Encoding.ASCII;
        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => Array.Empty<BenchmarkLogicalGroupRule>();

        public ConfigOptions Options => ConfigOptions.KeepBenchmarkFiles | ConfigOptions.DisableOptimizationsValidator;
    }
}