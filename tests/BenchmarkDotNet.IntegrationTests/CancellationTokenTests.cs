using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class CancellationTokenTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    [Fact]
    public void BenchmarkWithCancellationTokenProperty_ReceivesToken()
    {
        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry)
            .AddLogger(new OutputLogger(Output));

        CanExecute<BenchmarkWithCancellationToken>(config);
    }

    [Fact]
    public void BenchmarkWithCancellationTokenProperty_ReceivesToken_InProcessNoEmit()
    {
        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithToolchain(InProcessNoEmitToolchain.Default))
            .AddLogger(new OutputLogger(Output));

        CanExecute<BenchmarkWithCancellationToken>(config);
    }

    [Fact]
    public void BenchmarkWithCancellationTokenProperty_ReceivesToken_InProcessEmit()
    {
        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default))
            .AddLogger(new OutputLogger(Output));

        CanExecute<BenchmarkWithCancellationToken>(config);
    }

    [TheoryEnvSpecific("JSVU does not support ARM on Windows or Linux", EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm)]
    [InlineData("v8")]
    [InlineData("node")]
    public void BenchmarkWithCancellationTokenProperty_ReceivesToken_Wasm(string javaScriptEngine)
    {
        var dotnetVersion = "net8.0";
        var logger = new OutputLogger(Output);
        var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, runtimeFrameworkVersion: null!, "Wasm", aotCompilerMode: MonoAotCompilerMode.mini);

        var config = ManualConfig.CreateEmpty()
            .AddLogger(logger)
            .AddJob(Job.Dry
                .WithRuntime(new WasmRuntime(dotnetVersion, RuntimeMoniker.WasmNet80, "wasm", false, javaScriptEngine))
                .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
            .WithBuildTimeout(TimeSpan.FromSeconds(240))
            .WithOption(ConfigOptions.LogBuildOutput, true)
            .WithOption(ConfigOptions.GenerateMSBuildBinLog, false);

        CanExecute<BenchmarkWithCancellationToken>(config);
    }

    [Fact]
    public async Task RunWithCancellationTokenIsCancelled()
    {
        var cts = new CancellationTokenSource();
        var diagnoser = new CancelAfterFirstIterationDiagnoser(cts);

        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry)
            .AddLogger(new OutputLogger(Output))
            .AddDiagnoser(diagnoser);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await BenchmarkRunner.RunAsync<SimpleBenchmark>(config, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RunWithCancellationTokenIsCancelled_InProcessNoEmit()
    {
        var cts = new CancellationTokenSource();
        var diagnoser = new CancelAfterFirstIterationDiagnoser(cts);

        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithToolchain(InProcessNoEmitToolchain.Default))
            .AddLogger(new OutputLogger(Output))
            .AddDiagnoser(diagnoser);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await BenchmarkRunner.RunAsync<SimpleBenchmark>(config, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RunWithCancellationTokenIsCancelled_InProcessEmit()
    {
        var cts = new CancellationTokenSource();
        var diagnoser = new CancelAfterFirstIterationDiagnoser(cts);

        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default))
            .AddLogger(new OutputLogger(Output))
            .AddDiagnoser(diagnoser);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await BenchmarkRunner.RunAsync<SimpleBenchmark>(config, cancellationToken: cts.Token));
    }

    [Theory]
    [InlineDataEnvSpecific("v8", "JSVU does not support ARM on Windows or Linux", [EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm])]
    [InlineData("node")]
    public async Task RunWithCancellationTokenIsCancelled_Wasm(string javaScriptEngine)
    {
        var cts = new CancellationTokenSource();
        var diagnoser = new CancelAfterFirstIterationDiagnoser(cts);

        var dotnetVersion = "net8.0";
        var logger = new OutputLogger(Output);
        var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, runtimeFrameworkVersion: null!, "Wasm", aotCompilerMode: MonoAotCompilerMode.mini);

        var config = ManualConfig.CreateEmpty()
            .AddLogger(logger)
            .AddJob(Job.Dry
                .WithRuntime(new WasmRuntime(dotnetVersion, RuntimeMoniker.WasmNet80, "wasm", false, javaScriptEngine))
                .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
            .AddDiagnoser(diagnoser)
            .WithBuildTimeout(TimeSpan.FromSeconds(240))
            .WithOption(ConfigOptions.LogBuildOutput, true)
            .WithOption(ConfigOptions.GenerateMSBuildBinLog, false);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await BenchmarkRunner.RunAsync<SimpleBenchmark>(config, cancellationToken: cts.Token));
    }

    public class BenchmarkWithCancellationToken
    {
        [BenchmarkCancellation]
        public CancellationToken CancellationToken { get; set; }

        [Benchmark]
        public void CheckToken()
        {
            Assert.True(CancellationToken.CanBeCanceled);
            Assert.False(CancellationToken.IsCancellationRequested);
        }
    }

    public class SimpleBenchmark
    {
        [Benchmark]
        public void Empty() { }
    }

    public class CancelAfterFirstIterationDiagnoser(CancellationTokenSource cts) : IDiagnoser
    {
        public IEnumerable<string> Ids => [nameof(CancelAfterFirstIterationDiagnoser)];

        public IEnumerable<IExporter> Exporters => [];

        public IEnumerable<IAnalyser> Analysers => [];

        public BenchmarkDotNet.Diagnosers.RunMode GetRunMode(BenchmarkCase benchmarkCase) => BenchmarkDotNet.Diagnosers.RunMode.NoOverhead;

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => AsyncEnumerable.Empty<ValidationError>();

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeAnythingElse)
            {
                cts.Cancel();
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

        public void DisplayResults(ILogger logger) { }
    }
}
