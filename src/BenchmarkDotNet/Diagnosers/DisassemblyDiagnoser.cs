using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
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

        private readonly WindowsDisassembler windowsDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<BenchmarkCase, DisassemblyResult> results;

        private DisassemblyDiagnoser(WindowsDisassembler windowsDisassembler, MonoDisassembler monoDisassembler, DisassemblyDiagnoserConfig config)
        {
            Config = config;
            this.windowsDisassembler = windowsDisassembler;
            this.monoDisassembler = monoDisassembler;

            results = new Dictionary<BenchmarkCase, DisassemblyResult>();
            Exporters = GetExporters(results, config);
        }

        public static IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Create(DisassemblyDiagnoserConfig config)
            => new DisassemblyDiagnoser(new WindowsDisassembler(config), new MonoDisassembler(config), config);

        public IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Configure(DisassemblyDiagnoserConfig config)
            => Create(config);

        public IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> Results => results;
        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters { get; }

        public IEnumerable<IAnalyser> Analysers => new IAnalyser[] { new DisassemblyAnalyzer(Results) };

        public IEnumerable<Metric> ProcessResults(DiagnoserResults _) => Array.Empty<Metric>();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            if (ShouldUseWindowsDisassembler(benchmarkCase))
                return RunMode.NoOverhead;
            if (ShouldUseMonoDisassembler(benchmarkCase))
                return RunMode.SeparateLogic;

            return RunMode.None;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var benchmark = parameters.BenchmarkCase;

            switch (signal) {
                case HostSignal.AfterAll when ShouldUseWindowsDisassembler(benchmark):
                    results.Add(benchmark, windowsDisassembler.Disassemble(parameters));
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
                if (!RuntimeInformation.IsWindows() && !ShouldUseMonoDisassembler(benchmark))
                    yield return new ValidationError(false, "No Disassembler support, only Mono is supported for non-Windows OS", benchmark);

                if (benchmark.Job.Infrastructure.HasValue(InfrastructureMode.ToolchainCharacteristic)
                    && benchmark.Job.Infrastructure.Toolchain is InProcessToolchain)
                {
                    yield return new ValidationError(true, "InProcessToolchain has no DisassemblyDiagnoser support", benchmark);
                }
            }
        }

        private static bool ShouldUseMonoDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is MonoRuntime || RuntimeInformation.IsMono;

        private static bool ShouldUseWindowsDisassembler(BenchmarkCase benchmarkCase)
            => !(benchmarkCase.Job.Environment.Runtime is MonoRuntime) && RuntimeInformation.IsWindows();

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