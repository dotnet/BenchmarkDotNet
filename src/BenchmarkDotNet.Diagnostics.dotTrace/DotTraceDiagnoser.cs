using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Validators;
using RunMode = BenchmarkDotNet.Diagnosers.RunMode;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    public class DotTraceDiagnoser : IProfiler
    {
        private readonly Uri? nugetUrl;
        private readonly string? toolsDownloadFolder;

        public DotTraceDiagnoser(Uri? nugetUrl = null, string? toolsDownloadFolder = null)
        {
            this.nugetUrl = nugetUrl;
            this.toolsDownloadFolder = toolsDownloadFolder;
        }

        public IEnumerable<string> Ids => new[] { "DotTrace" };
        public string ShortName => "dotTrace";

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            return IsSupported(benchmarkCase.Job.Environment.GetRuntime().RuntimeMoniker) ? RunMode.ExtraRun : RunMode.None;
        }

        private readonly List<string> snapshotFilePaths = new ();

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var job = parameters.BenchmarkCase.Job;
            bool isInProcess = job.GetToolchain().IsInProcess;
            var logger = parameters.Config.GetCompositeLogger();
            DotTraceToolBase tool = isInProcess
                ? new InProcessDotTraceTool(logger, nugetUrl, downloadTo: toolsDownloadFolder)
                : new ExternalDotTraceTool(logger, nugetUrl, downloadTo: toolsDownloadFolder);

            var runtimeMoniker = job.Environment.GetRuntime().RuntimeMoniker;
            if (!IsSupported(runtimeMoniker))
            {
                logger.WriteLineError($"Runtime '{runtimeMoniker}' is not supported by dotTrace");
                return;
            }

            switch (signal)
            {
                case HostSignal.BeforeAnythingElse:
                    tool.Init(parameters);
                    break;
                case HostSignal.BeforeActualRun:
                    snapshotFilePaths.Add(tool.Start(parameters));
                    break;
                case HostSignal.AfterActualRun:
                    tool.Stop(parameters);
                    break;
            }
        }

        public IEnumerable<IExporter> Exporters => Enumerable.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Enumerable.Empty<IAnalyser>();

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var runtimeMonikers = validationParameters.Benchmarks.Select(b => b.Job.Environment.GetRuntime().RuntimeMoniker).Distinct();
            foreach (var runtimeMoniker in runtimeMonikers)
            {
                if (!IsSupported(runtimeMoniker))
                    yield return new ValidationError(true, $"Runtime '{runtimeMoniker}' is not supported by dotTrace");
            }
        }

        internal static bool IsSupported(RuntimeMoniker runtimeMoniker)
        {
            switch (runtimeMoniker)
            {
                case RuntimeMoniker.HostProcess:
                case RuntimeMoniker.Net461:
                case RuntimeMoniker.Net462:
                case RuntimeMoniker.Net47:
                case RuntimeMoniker.Net471:
                case RuntimeMoniker.Net472:
                case RuntimeMoniker.Net48:
                case RuntimeMoniker.Net481:
                case RuntimeMoniker.Net50:
                case RuntimeMoniker.Net60:
                case RuntimeMoniker.Net70:
                case RuntimeMoniker.Net80:
                case RuntimeMoniker.Net90:
                    return true;
                case RuntimeMoniker.NotRecognized:
                case RuntimeMoniker.Mono:
                case RuntimeMoniker.NativeAot60:
                case RuntimeMoniker.NativeAot70:
                case RuntimeMoniker.NativeAot80:
                case RuntimeMoniker.NativeAot90:
                case RuntimeMoniker.Wasm:
                case RuntimeMoniker.WasmNet50:
                case RuntimeMoniker.WasmNet60:
                case RuntimeMoniker.WasmNet70:
                case RuntimeMoniker.WasmNet80:
                case RuntimeMoniker.WasmNet90:
                case RuntimeMoniker.MonoAOTLLVM:
                case RuntimeMoniker.MonoAOTLLVMNet60:
                case RuntimeMoniker.MonoAOTLLVMNet70:
                case RuntimeMoniker.MonoAOTLLVMNet80:
                case RuntimeMoniker.MonoAOTLLVMNet90:
                case RuntimeMoniker.Mono60:
                case RuntimeMoniker.Mono70:
                case RuntimeMoniker.Mono80:
                case RuntimeMoniker.Mono90:
#pragma warning disable CS0618 // Type or member is obsolete
                case RuntimeMoniker.NetCoreApp50:
#pragma warning restore CS0618 // Type or member is obsolete
                    return false;
                case RuntimeMoniker.NetCoreApp20:
                case RuntimeMoniker.NetCoreApp21:
                case RuntimeMoniker.NetCoreApp22:
                    return RuntimeInformation.IsWindows();
                case RuntimeMoniker.NetCoreApp30:
                case RuntimeMoniker.NetCoreApp31:
                    return RuntimeInformation.IsWindows() || RuntimeInformation.IsLinux();
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeMoniker), runtimeMoniker, $"Runtime moniker {runtimeMoniker} is not supported");
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => ImmutableArray<Metric>.Empty;

        public void DisplayResults(ILogger logger)
        {
            if (snapshotFilePaths.Any())
            {
                logger.WriteLineInfo("The following dotTrace snapshots were generated:");
                foreach (string snapshotFilePath in snapshotFilePaths)
                    logger.WriteLineInfo($"* {snapshotFilePath}");
            }
        }
    }
}