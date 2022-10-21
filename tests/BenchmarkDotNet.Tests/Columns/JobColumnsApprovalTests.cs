using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
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
using JetBrains.Annotations;
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

        [UsedImplicitly]
        public static TheoryData<IConfig> GetConfigs()
        {
            var data = new TheoryData<IConfig>
            {
                DefaultConfig.Instance
                    .AddJob(Job.InProcess),

                DefaultConfig.Instance
                    .AddJob(Job.Default)
                    .AddJob(Job.InProcess),

                DefaultConfig.Instance
                    .AddJob(Job.Dry.WithRuntime(CoreRuntime.Core70).WithId("net7")),

                DefaultConfig.Instance
                    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core70).WithId("net7"))
                    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60).WithId("net6")),

                DefaultConfig.Instance
                    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core70).WithId("net7"))
                    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60).WithId("net6").WithToolchain(InProcessEmitToolchain.Instance))
                    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60).WithId("net6")),
            };

            return data;
        }

        [Theory]
        [MemberData(nameof(GetConfigs))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ColumnsDisplayTest(IConfig config)
        {
            var fileName = string.Join("-", config.GetJobs());

            NamerFactory.AdditionalInformation = fileName;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + fileName + " ===");

            var exporter = MarkdownExporter.Mock;
            var summary = MockFactory.CreateSummary<BenchmarkClass>(config);
            exporter.ExportToLog(summary, logger);

            Approvals.Verify(logger.GetLog());
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        public class BenchmarkClass
        {
            [Benchmark] public void Method() { }
        }

        [FactDotNetCoreOnly("In the .Net Framework cmd job uses CsProjClassicNetToolchain while fluent and attribute jobs use RoslynToolchain by default")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MultipleInputColumnsDisplayTest()
        {
            var cmdConfig = ConfigParser.Parse(
                "--join --runtimes net481 net6.0 nativeaot6.0".Split(), NullLogger.Instance).config;

            var fluentConfig = ManualConfig.CreateEmpty().AddColumnProvider(DefaultColumnProviders.Instance)
                .AddJob(Job.Dry.WithRuntime(CoreRuntime.Core60))
                .AddJob(Job.Dry.WithRuntime(ClrRuntime.Net481))
                .AddJob(Job.Dry.WithRuntime(NativeAotRuntime.Net60));

            var config = ManualConfig.Union(cmdConfig, fluentConfig);

            NamerFactory.AdditionalInformation = nameof(MultipleInputColumnsDisplayTest);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + nameof(MultipleInputColumnsDisplayTest) + " ===");

            var exporter = MarkdownExporter.Mock;
            var summary = MockFactory.CreateSummary<BenchmarkClass1>(config);
            exporter.ExportToLog(summary, logger);

            var log = logger.GetLog();
            log = ReplaceRandomIDs(log);
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

        [SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net60)]
        [SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net481)]
        [SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.NativeAot60)]
        public class BenchmarkClass1
        {
            [Benchmark] public void Method() { }
        }
    }
}