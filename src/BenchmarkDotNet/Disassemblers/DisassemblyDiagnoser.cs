using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Disassemblers.Exporters;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
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
        private static readonly Lazy<string> ptrace_scope = new Lazy<string>(() => ProcessHelper.RunAndReadOutput("cat", "/proc/sys/kernel/yama/ptrace_scope").Trim());

        private readonly WindowsDisassembler windowsDisassembler;
        private readonly LinuxDisassembler linuxDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<BenchmarkCase, DisassemblyResult> results;

        public DisassemblyDiagnoser(DisassemblyDiagnoserConfig config)
        {
            Config = config;
            windowsDisassembler = new WindowsDisassembler(config);
            linuxDisassembler = new LinuxDisassembler(config);
            monoDisassembler = new MonoDisassembler(config);

            results = new Dictionary<BenchmarkCase, DisassemblyResult>();
            Exporters = GetExporters(results, config);
        }

        public DisassemblyDiagnoserConfig Config { get; }

        public IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> Results => results;

        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters { get; }

        public IEnumerable<IAnalyser> Analysers => new IAnalyser[] { new DisassemblyAnalyzer(results) };

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
                    ? "Disassembled benchmarks got exported to \".\\BenchmarkDotNet.Artifacts\\results\\*asm.md\""
                    : "No benchmarks were disassembled");

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

                if (ShouldUseLinuxDisassembler(benchmark))
                {
                    var runtime = benchmark.Job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance);

                    if (runtime.RuntimeMoniker < RuntimeMoniker.NetCoreApp30)
                    {
                        yield return new ValidationError(true, $"{nameof(DisassemblyDiagnoser)} supports only .NET Core 3.0+", benchmark);
                    }

                    if (ptrace_scope.Value == "2")
                    {
                        yield return new ValidationError(false, $"ptrace_scope is set to 2, {nameof(DisassemblyDiagnoser)} is going to work only if you run as sudo");
                    }
                    else if (ptrace_scope.Value == "3")
                    {
                        yield return new ValidationError(true, $"ptrace_scope is set to 3, {nameof(DisassemblyDiagnoser)} is not going to work");
                    }
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
            if (config.ExportGithubMarkdown)
            {
                yield return new GithubMarkdownDisassemblyExporter(results, config);
            }
            if (config.ExportHtml)
            {
                yield return new HtmlDisassemblyExporter(results, config);
            }
            if (config.ExportCombinedDisassemblyReport)
            {
                yield return new CombinedDisassemblyExporter(results, config);
            }
            if (config.ExportDiff)
            {
                yield return new GithubMarkdownDiffDisassemblyExporter(results, config);
            }
        }

        private static long SumNativeCodeSize(DisassemblyResult disassembly)
            => disassembly.Methods.Sum(method => method.Maps.Sum(map => map.SourceCodes.OfType<Asm>().Sum(asm => asm.Instruction.Length)));

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