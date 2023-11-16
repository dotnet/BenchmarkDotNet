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

        private readonly WindowsDisassembler windowsDifferentArchitectureDisassembler;
        private readonly SameArchitectureDisassembler sameArchitectureDisassembler;
        private readonly MonoDisassembler monoDisassembler;
        private readonly Dictionary<BenchmarkCase, DisassemblyResult> results;

        public DisassemblyDiagnoser(DisassemblyDiagnoserConfig config)
        {
            Config = config;
            windowsDifferentArchitectureDisassembler = new WindowsDisassembler(config);
            sameArchitectureDisassembler = new SameArchitectureDisassembler(config);
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
            if (ShouldUseClrMdDisassembler(benchmarkCase))
                return RunMode.NoOverhead;
            else if (ShouldUseMonoDisassembler(benchmarkCase))
                return RunMode.SeparateLogic;

            return RunMode.None;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var benchmark = parameters.BenchmarkCase;

            switch (signal)
            {
                case HostSignal.AfterAll when ShouldUseSameArchitectureDisassembler(benchmark, parameters):
                    results.Add(benchmark, sameArchitectureDisassembler.Disassemble(parameters));
                    break;
                case HostSignal.AfterAll when RuntimeInformation.IsWindows() && !ShouldUseMonoDisassembler(benchmark):
                    results.Add(benchmark, windowsDifferentArchitectureDisassembler.Disassemble(parameters));
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
            if (!(currentPlatform is Platform.X64 or Platform.X86 or Platform.Arm64))
            {
                yield return new ValidationError(true, $"{currentPlatform} is not supported");
                yield break;
            }

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark.Job.Infrastructure.TryGetToolchain(out var toolchain) && toolchain is InProcessNoEmitToolchain)
                {
                    yield return new ValidationError(true, "InProcessToolchain has no DisassemblyDiagnoser support", benchmark);
                }
                else if (benchmark.Job.IsNativeAOT())
                {
                    yield return new ValidationError(true, "Currently NativeAOT has no DisassemblyDiagnoser support", benchmark);
                }

                if (ShouldUseClrMdDisassembler(benchmark))
                {
                    if (RuntimeInformation.IsLinux())
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
                else if (!ShouldUseMonoDisassembler(benchmark))
                {
                    yield return new ValidationError(true, $"Only Windows and Linux are supported in DisassemblyDiagnoser without Mono. Current OS is {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
                }
            }
        }

        private static bool ShouldUseMonoDisassembler(BenchmarkCase benchmarkCase)
            => benchmarkCase.Job.Environment.Runtime is MonoRuntime || RuntimeInformation.IsMono;

        // when we add  macOS support, RuntimeInformation.IsMacOS() needs to be added here
        private static bool ShouldUseClrMdDisassembler(BenchmarkCase benchmarkCase)
            => !ShouldUseMonoDisassembler(benchmarkCase) && (RuntimeInformation.IsWindows() || RuntimeInformation.IsLinux());

        private static bool ShouldUseSameArchitectureDisassembler(BenchmarkCase benchmarkCase, DiagnoserActionParameters parameters)
        {
            if (ShouldUseClrMdDisassembler(benchmarkCase))
            {
                if (RuntimeInformation.IsWindows())
                {
                    return WindowsDisassembler.GetDisassemblerArchitecture(parameters.Process, benchmarkCase.Job.Environment.Platform)
                        == RuntimeInformation.GetCurrentPlatform();
                }

                // on Unix currently host process architecture is always the same as benchmark process architecture
                // (no official x86 support)
                return true;
            }

            return false;
        }

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
            => disassembly.Methods.Sum(method => method.Maps.Sum(map => map.SourceCodes.OfType<Asm>().Sum(asm => asm.InstructionLength)));

        private class NativeCodeSizeMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new NativeCodeSizeMetricDescriptor();

            public string Id => "Native Code Size";
            public string DisplayName => Column.CodeSize;
            public string Legend => "Native code size of the disassembled method(s)";
            public string NumberFormat => "N0";
            public UnitType UnitType => UnitType.CodeSize;
            public string Unit => SizeUnit.B.Name;
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }
    }
}