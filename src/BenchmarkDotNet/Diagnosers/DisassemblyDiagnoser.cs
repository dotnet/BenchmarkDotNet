using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoser : IDisassemblyDiagnoser
    {
        private readonly WindowsDisassembler windowsDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<BenchmarkCase, DisassemblyResult> results;

        internal DisassemblyDiagnoser(WindowsDisassembler windowsDisassembler, MonoDisassembler monoDisassembler)
        {
            this.windowsDisassembler = windowsDisassembler;
            this.monoDisassembler = monoDisassembler;

            results = new Dictionary<BenchmarkCase, DisassemblyResult>();
            Exporters = new IExporter[]
            {
                new CombinedDisassemblyExporter(results),
                new RawDisassemblyExporter(results),
                new PrettyDisassemblyExporter(results)
            };
        }

        public static IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Create(DisassemblyDiagnoserConfig config)
            => new DisassemblyDiagnoser(new WindowsDisassembler(config), new MonoDisassembler(config));

        public IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Configure(DisassemblyDiagnoserConfig config)
            => Create(config);

        public IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> Results => results;
        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters { get; }

        public IEnumerable<IAnalyser> Analysers => new IAnalyser[] { new DisassemblyAnalyzer(Results) };

        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;
        public void ProcessResults(DiagnoserResults _) { }

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

            if (signal == HostSignal.AfterAll && ShouldUseWindowsDisassembler(benchmark))
                results.Add(benchmark, windowsDisassembler.Disassemble(parameters));
            else if (signal == HostSignal.SeparateLogic && ShouldUseMonoDisassembler(benchmark))
                results.Add(benchmark, monoDisassembler.Disassemble(benchmark, benchmark.Job.Environment.Runtime as MonoRuntime));
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

        private bool ShouldUseMonoDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is MonoRuntime || RuntimeInformation.IsMono;

        private bool ShouldUseWindowsDisassembler(BenchmarkCase benchmarkCase)
            => !(benchmarkCase.Job.Environment.Runtime is MonoRuntime) && RuntimeInformation.IsWindows();
    }
}