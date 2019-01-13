using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers.DisassemblerDataContracts;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoser : IDisassemblyDiagnoser
    {
        public DisassemblyDiagnoserConfig Config { get; }

        private readonly ManagedDotNetDisassembler managedDotNetDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<BenchmarkCase, DisassemblyResult> results;

        private DisassemblyDiagnoser(ManagedDotNetDisassembler managedDotNetDisassembler, MonoDisassembler monoDisassembler, DisassemblyDiagnoserConfig config)
        {
            Config = config;
            this.managedDotNetDisassembler = managedDotNetDisassembler;
            this.monoDisassembler = monoDisassembler;

            results = new Dictionary<BenchmarkCase, DisassemblyResult>();
            Exporters = GetExporters(results, config);
        }

        public static IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Create(DisassemblyDiagnoserConfig config)
            => new DisassemblyDiagnoser(new ManagedDotNetDisassembler(config), new MonoDisassembler(config), config);

        public IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Configure(DisassemblyDiagnoserConfig config)
            => Create(config);

        public IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> Results => results;
        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters { get; }

        public IEnumerable<IAnalyser> Analysers => new IAnalyser[] { new DisassemblyAnalyzer(Results) };

        public IEnumerable<Metric> ProcessResults(DiagnoserResults _) => Array.Empty<Metric>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            if (ShouldManagedDotNetDisassembler(benchmarkCase))
                return RunMode.NoOverhead;
            if (ShouldUseMonoDisassembler(benchmarkCase))
                return RunMode.SeparateLogic;

            return RunMode.None;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var benchmark = parameters.BenchmarkCase;

            switch (signal) {
                case HostSignal.AfterAll when ShouldManagedDotNetDisassembler(benchmark):
                    results.Add(benchmark, managedDotNetDisassembler.Disassemble(parameters));
                    break;
                case HostSignal.SeparateLogic when ShouldUseMonoDisassembler(benchmark):
                    results.Add(benchmark, monoDisassembler.Disassemble(benchmark, benchmark.Job.Environment.Runtime as MonoRuntime));
                    break;
            }
        }

        public void DisplayResults(ILogger logger)
            => logger.WriteInfo(
                results.Any()
                    ? "The results were exported to \".\\BenchmarkDotNet.Artifacts\\results\\*-disassembly-report.html\""
                    : "No results were exported");

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                // TODO: add verification for global tool installed

                if (ShouldUseNativeDisassembler(benchmark))
                    yield return new ValidationError(false, "No native Disassembler support (yet!)", benchmark);

                if (ShouldUseArmDisassembler(benchmark))
                    yield return new ValidationError(false, "No ARM Disassembler support (yet!)", benchmark);

                if (benchmark.Job.Infrastructure.HasValue(InfrastructureMode.ToolchainCharacteristic)
                    && benchmark.Job.Infrastructure.Toolchain is InProcessToolchain)
                {
                    yield return new ValidationError(true, "InProcessToolchain has no DisassemblyDiagnoser support", benchmark);
                }
            }
        }

        private static bool ShouldUseNativeDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is CoreRtRuntime || RuntimeInformation.IsCoreRT || RuntimeInformation.IsNetNative;

        private static bool ShouldUseArmDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Platform.IsArm() || RuntimeInformation.GetCurrentPlatform().IsArm();

        private static bool ShouldUseMonoDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is MonoRuntime || RuntimeInformation.IsMono;

        private static bool ShouldManagedDotNetDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is CoreRuntime || RuntimeInformation.IsNetCore
            || benchmarkCase.Job.Environment.Runtime is ClrRuntime || RuntimeInformation.IsFullFramework;

        private static IEnumerable<IExporter> GetExporters(Dictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            yield return new CombinedDisassemblyExporter(results);
            yield return new RawDisassemblyExporter(results);
            yield return new PrettyHtmlDisassemblyExporter(results);
            yield return new PrettyGithubMarkdownDisassemblyExporter(results);

            if (config.PrintDiff)
            {
                yield return new PrettyGithubMarkdownDiffDisassemblyExporter(results);
            }
        }
    }
}