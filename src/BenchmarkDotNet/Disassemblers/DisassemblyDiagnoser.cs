using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Disassemblers.Exporters;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoser : IDiagnoser
    {
        public DisassemblyDiagnoserConfig Config { get; }

        private readonly WindowsDisassembler windowsDisassembler;
        private readonly LinuxDisassembler linuxDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<BenchmarkCase, DisassemblyResult> results;

        private DisassemblyDiagnoser(WindowsDisassembler windowsDisassembler, LinuxDisassembler linuxDisassembler, MonoDisassembler monoDisassembler, DisassemblyDiagnoserConfig config)
        {
            Config = config;
            this.windowsDisassembler = windowsDisassembler;
            this.linuxDisassembler = linuxDisassembler;
            this.monoDisassembler = monoDisassembler;

            results = new Dictionary<BenchmarkCase, DisassemblyResult>();
            Exporters = GetExporters(results, config);
        }

        public static DisassemblyDiagnoser Create(DisassemblyDiagnoserConfig config)
            => new DisassemblyDiagnoser(new WindowsDisassembler(config), new LinuxDisassembler(config), new MonoDisassembler(config), config);

        public IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> Results => results;

        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters { get; }

        public IEnumerable<IAnalyser> Analysers => new IAnalyser[] { new DisassemblyAnalyzer(Results) };

        public IEnumerable<Metric> ProcessResults(DiagnoserResults diagnoserResults)
        {
            if (results.TryGetValue(diagnoserResults.BenchmarkCase, out var disassemblyResult))
                yield return new Metric(NativeCodeSizeMetricDescriptor.Instance, SumNativeCodeSize(disassemblyResult));
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            if (ShouldUseWindowsDisassembler(benchmarkCase) || ShouldUseLinuxDisassembler(benchmarkCase))
                return RunMode.NoOverhead;
            if (ShouldUseMonoDisassembler(benchmarkCase))
                return RunMode.SeparateLogic;

            return RunMode.None;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var benchmark = parameters.BenchmarkCase;

            switch (signal)
            {
                case HostSignal.AfterAll when ShouldUseWindowsDisassembler(benchmark):
                    results.Add(benchmark, windowsDisassembler.Disassemble(parameters));
                    break;
                case HostSignal.AfterAll when ShouldUseLinuxDisassembler(benchmark):
                    results.Add(benchmark, linuxDisassembler.Disassemble(parameters));
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
            var currentPlatform = RuntimeInformation.GetCurrentPlatform();
            if (currentPlatform != Platform.X64 && currentPlatform != Platform.X86)
            {
                yield return new ValidationError(true, $"{currentPlatform} is not supported (Iced library limitation)");
                yield break;
            }

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark.Job.Infrastructure.HasValue(InfrastructureMode.ToolchainCharacteristic) && benchmark.Job.Infrastructure.Toolchain is InProcessNoEmitToolchain)
                {
                    yield return new ValidationError(true, "InProcessToolchain has no DisassemblyDiagnoser support", benchmark);
                }
            }
        }

        private static bool ShouldUseMonoDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is MonoRuntime || RuntimeInformation.IsMono;

        private static bool ShouldUseWindowsDisassembler(BenchmarkCase benchmarkCase)
            => !(benchmarkCase.Job.Environment.Runtime is MonoRuntime) && RuntimeInformation.IsWindows();
        
        private static bool ShouldUseLinuxDisassembler(BenchmarkCase benchmarkCase)
            => !(benchmarkCase.Job.Environment.Runtime is MonoRuntime) && RuntimeInformation.IsLinux();

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

        private static long SumNativeCodeSize(DisassemblyResult disassembly)
            => disassembly.Methods.Sum(method => method.Maps.Sum(map => map.Instructions.OfType<Asm>().Sum(asm => asm.SizeInBytes)));

        private class NativeCodeSizeMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new NativeCodeSizeMetricDescriptor();

            public string Id => "Native Code Size";
            public string DisplayName => "Code Size";
            public string Legend => "Native code size of the disassembled method(s)";
            public string NumberFormat => "N0";
            public UnitType UnitType => UnitType.Size;
            public string Unit => SizeUnit.B.Name;
            public bool TheGreaterTheBetter => false;
        }
    }
}