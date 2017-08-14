using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
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
        public IEnumerable<IExporter> Exporters => new[] { new DisassemblyExporter(Results) };

        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;
        public void BeforeAnythingElse(DiagnoserActionParameters parameters) { }
        public void BeforeMainRun(DiagnoserActionParameters parameters) { }
        public void BeforeGlobalCleanup(DiagnoserActionParameters parameters) { }

        public RunMode GetRunMode(Benchmark benchmark)
        {
            if (ShouldUseWindowsDissasembler(benchmark))
                return RunMode.ExtraRun;
            if (ShouldUseMonoDisassembler(benchmark))
                return RunMode.SeparateLogic;

            return RunMode.None;
        }

        // method was already compiled and executed for the Warmup, we can attach to the process and do the job
        public void AfterGlobalSetup(DiagnoserActionParameters parameters)
        {
#if CLASSIC
            if (ShouldUseWindowsDissasembler(parameters.Benchmark))
                results.Add(
                    parameters.Benchmark,
                    windowsDisassembler.Dissasemble(
                        parameters, 
                        DiagnosersLoader.LoadDiagnosticsAssembly(typeof(DisassemblyDiagnoser).GetTypeInfo().Assembly)));
#endif
        }

        // no need to run benchmarks once again, just do this after all runs
        public void ProcessResults(Benchmark benchmark, BenchmarkReport report)
        {
            if (ShouldUseMonoDisassembler(benchmark))
                results.Add(benchmark, monoDisassembler.Disassemble(benchmark, benchmark.Job.Env.Runtime as MonoRuntime));
        }

        public void DisplayResults(ILogger logger)
            => logger.WriteInfo(
                results.Any()
                    ? "The results were exported to \".\\BenchmarkDotNet.Artifacts\\results\\*-disassembly-report.html\""
                    : "No results were exported");

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
#if CORE
            yield return new ValidationError(false, "To use the classic Windows diagnosers for .NET Core you need to run the benchmarks for desktop .NET. More info: http://adamsitnik.com/Hardware-Counters-Diagnoser/#how-to-get-it-running-for-net-coremono-on-windows");
            yield break;
#endif

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (!RuntimeInformation.IsWindows() && !ShouldUseMonoDisassembler(benchmark))
                    yield return new ValidationError(false, "No Disassebler support, only Mono is supported for non-Windows OS", benchmark);

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