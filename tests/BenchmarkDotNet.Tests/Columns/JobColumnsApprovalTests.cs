using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns
{
    // In case of failed approval tests, use the following reporter:
    // [UseReporter(typeof(KDiffReporter))]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    [Collection("ApprovalTests")]
    public class JobColumnsApprovalTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public JobColumnsApprovalTests() => initCulture = Thread.CurrentThread.CurrentCulture;

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ImplicitIdsAreDiscovered()
        {
            var config = DefaultConfig.Instance.AddJob(Job.Dry);

            Verify<Benchmark>(config);
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ExplicitIdsAreDiscovered()
        {
            var config = DefaultConfig.Instance.AddJob(Job.Dry.WithId("Dry"));

            Verify<Benchmark>(config);
        }

        [DryJob(RuntimeMoniker.Net60)]
        [SimpleJob(RuntimeMoniker.Net60)]
        [SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net60, 1, 1, 1)]
        public class BenchmarkWithAttributeJobs
        {
            [Benchmark] public void Method() { }
        }

        [Fact]
        public void ToolchainIsDiscovered()
        {
            var config = DefaultConfig.Instance.AddJob(Job.InProcess);

            Verify<Benchmark>(config);
        }

        [Fact]
        public void ToolchainsAreDiscovered()
        {
            var config = DefaultConfig.Instance
                .AddJob(Job.Default)
                .AddJob(Job.InProcess);

            Verify<Benchmark>(config);
        }

        [Fact]
        public void RuntimeIsDiscovered()
        {
            var config = DefaultConfig.Instance
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core70));

            Verify<Benchmark>(config);
        }

        [Fact]
        public void RuntimesAreDiscovered()
        {
            var config = DefaultConfig.Instance
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core70))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60));

            Verify<Benchmark>(config);
        }

        [Fact]
        public void RuntimesWithToolchainsAreDiscovered()
        {
            var config = DefaultConfig.Instance
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core70).WithToolchain(InProcessEmitToolchain.Instance))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60));

            Verify<Benchmark>(config);
        }

        [FactDotNetCoreOnly("In the .Net Framework cmd job uses CsProjClassicNetToolchain while fluent and attribute jobs use RoslynToolchain by default")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MultipleInputJobsAreDiscovered()
        {
            var cmdConfig = ConfigParser.Parse(
                "--join --runtimes net481 net7.0 nativeaot7.0".Split(), NullLogger.Instance).config;

            var fluentConfig = DefaultConfig.Instance
                .AddJob(Job.Dry.WithRuntime(CoreRuntime.Core70))
                .AddJob(Job.Dry.WithRuntime(ClrRuntime.Net481))
                .AddJob(Job.Dry.WithRuntime(NativeAotRuntime.Net70));

            var config = ManualConfig.Union(cmdConfig, fluentConfig);

            Verify<BenchmarkWithDryJobs>(config);
        }

        public class Benchmark
        {
            [Benchmark] public void Method() { }
        }

        [DryJob(RuntimeMoniker.Net60)]
        [DryJob(RuntimeMoniker.Net481)]
        [DryJob(RuntimeMoniker.NativeAot60)]
        public class BenchmarkWithDryJobs
        {
            [Benchmark] public void Method() { }
        }

        private static void Verify<TBenchmark>(IConfig config)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var exporter = MarkdownExporter.Mock;
            var summary = MockFactory.CreateSummary<TBenchmark>(config);

            var logger = new AccumulationLogger();
            exporter.ExportToLog(summary, logger);

            var log = ReplaceRandomIDs(logger.GetLog());
            Approvals.Verify(log);
        }

        private static string ReplaceRandomIDs(string log)
        {
            var regex = new Regex(@"Job-\w*");

            var index = 0;
            foreach (Match match in regex.Matches(log))
            {
                var randomGeneratedJobName = match.Value;

                // JobIdGenerator.GenerateRandomId() generates Job-ABCDEF
                // respect the length for proper table formatting
                var persistantName = $"Job-rndId{index}";
                log = log.Replace(randomGeneratedJobName, persistantName);
                index++;
            }

            return log;
        }
    }
}