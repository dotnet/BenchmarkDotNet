using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SignalsTests : BenchmarkTestExecutor
    {
        public SignalsTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        public static IEnumerable<object[]> GetToolchains()
        {
            yield return new object[] { InProcessEmitToolchain.Instance };

            // host.Runtime == benchmark.Runtime
            yield return new object[] { Job.Default.GetToolchain() };

            if (RuntimeInformation.IsWindows())
            {
                // on Windows where .NET Core and .NET might use different encodings (#1487), we also want to have:
                // .NET Core host and Full .NET Framework benchmark
                if (RuntimeInformation.IsNetCore)
                    yield return new object[] { Job.Default.WithRuntime(ClrRuntime.Net461).GetToolchain() };
                // Full .NET Framework host and .NET Core benchmark
                else if (RuntimeInformation.IsFullFramework)
                    yield return new object[] { Job.Default.WithRuntime(CoreRuntime.Core21).GetToolchain() };
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void DiagnoserGetsAllSignals(IToolchain toolchain)
        {
            var diagnoser = new FakeDiagnoser();
            var job = Job.Dry // just run the code once!
                .WithToolchain(toolchain);

            // the dotnet test -f net461 runs as x86, but we only have x64 SDK installed
            if (RuntimeInformation.IsWindows() && RuntimeInformation.IsFullFramework && RuntimeInformation.GetCurrentPlatform() == Platform.X86)
                job = job.WithPlatform(Platform.X64);

            var config = ManualConfig.CreateEmpty()
                .AddJob(job)
                .AddDiagnoser(diagnoser);

            CanExecute<JustEmptyBenchmark>(config);

            Assert.Equal(toolchain is InProcessEmitToolchain ? -1 : 1, diagnoser.GetInvokeCount(HostSignal.BeforeProcessStart));
            Assert.Equal(1, diagnoser.GetInvokeCount(HostSignal.BeforeAnythingElse));
            Assert.Equal(1, diagnoser.GetInvokeCount(HostSignal.BeforeActualRun));
            Assert.Equal(1, diagnoser.GetInvokeCount(HostSignal.AfterActualRun));
            Assert.Equal(1, diagnoser.GetInvokeCount(HostSignal.AfterAll));
            Assert.Equal(toolchain is InProcessEmitToolchain ? -1 : 1, diagnoser.GetInvokeCount(HostSignal.AfterProcessExit));
        }

        public class JustEmptyBenchmark
        {
            [Benchmark] public void Nothing() { }
        }

        internal class FakeDiagnoser : IDiagnoser
        {
            private Dictionary<HostSignal, int> invokeCounts = new Dictionary<HostSignal, int>();

            public IEnumerable<string> Ids => new[] { nameof(FakeDiagnoser) };
            public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
            public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

            public void DisplayResults(ILogger logger) { }
            public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => Array.Empty<Metric>();
            public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();
            public BenchmarkDotNet.Diagnosers.RunMode GetRunMode(BenchmarkCase benchmarkCase) => BenchmarkDotNet.Diagnosers.RunMode.NoOverhead;

            public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
            {
                int before = invokeCounts.TryGetValue(signal, out int count) ? count : 0;

                invokeCounts[signal] = before + 1;
            }

            internal int GetInvokeCount(HostSignal signal) => invokeCounts.TryGetValue(signal, out int count) ? count : -1;
        }
    }
}
