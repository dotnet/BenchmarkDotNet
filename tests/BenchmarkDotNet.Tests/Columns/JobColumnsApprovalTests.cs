using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
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
    }
}