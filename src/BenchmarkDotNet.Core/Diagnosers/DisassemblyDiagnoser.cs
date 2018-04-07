using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly WindowsDisassembler windowsDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<Benchmark, DisassemblyResult> results = new Dictionary<Benchmark, DisassemblyResult>();

        internal DisassemblyDiagnoser(WindowsDisassembler windowsDisassembler, MonoDisassembler monoDisassembler)
        {
            this.windowsDisassembler = windowsDisassembler;
            this.monoDisassembler = monoDisassembler;
        }

        public static IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Create(DisassemblyDiagnoserConfig config)
            => new DisassemblyDiagnoser(new WindowsDisassembler(config), new MonoDisassembler(config));

        public IConfigurableDiagnoser<DisassemblyDiagnoserConfig> Configure(DisassemblyDiagnoserConfig config)
            => Create(config);

        public IReadOnlyDictionary<Benchmark, DisassemblyResult> Results => results;
        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters 
            => new IExporter[]
            {
                new CombinedDisassemblyExporter(Results),
                new RawDisassemblyExporter(Results),
                new PrettyDisassemblyExporter(Results)
            };

        public IEnumerable<IAnalyser> Analysers => new IAnalyser[] { new DisassemblyAnalyzer(Results) };

        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;
        public void ProcessResults(DiagnoserResults _) { }

        public RunMode GetRunMode(Benchmark benchmark)
        {
            if (ShouldUseWindowsDissasembler(benchmark))
                return RunMode.NoOverhead;
            if (ShouldUseMonoDisassembler(benchmark))
                return RunMode.SeparateLogic;

            return RunMode.None;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var benchmark = parameters.Benchmark;

            if (signal == HostSignal.AfterAll && ShouldUseWindowsDissasembler(benchmark))
                results.Add(benchmark, windowsDisassembler.Dissasemble(parameters));
            else if (signal == HostSignal.SeparateLogic && ShouldUseMonoDisassembler(benchmark))
                results.Add(benchmark, monoDisassembler.Disassemble(benchmark, benchmark.Job.Env.Runtime as MonoRuntime));
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

        private bool ShouldUseMonoDisassembler(Benchmark benchmark)
            => benchmark.Job.Env.Runtime is MonoRuntime || RuntimeInformation.IsMono();

        private bool ShouldUseWindowsDissasembler(Benchmark benchmark)
            => !(benchmark.Job.Env.Runtime is MonoRuntime) && RuntimeInformation.IsWindows();
    }
}