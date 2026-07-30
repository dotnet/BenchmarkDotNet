using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ArtifactNamingTests : BenchmarkTestExecutor
    {
        private const string CustomTitle = "MyCustomTitle";
        private const string FirstTypeName = "BenchmarkDotNet.IntegrationTests.FirstBenchmarks";
        private const string SecondTypeName = "BenchmarkDotNet.IntegrationTests.SecondBenchmarks";

        public ArtifactNamingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ArtifactsAreNamedAfterTheBenchmarkedTypesWhenNoTitleIsSet()
        {
            using var artifacts = new TempFolder();

            RunBenchmarks(artifacts, title: null, join: false, typeof(FirstBenchmarks), typeof(SecondBenchmarks));

            Assert.Equal([$"{FirstTypeName}-report-github.md", $"{SecondTypeName}-report-github.md"], GetExportedFiles(artifacts));
            // the log file of every run is unique, so that --resume can tell the previous run from the current one
            Assert.Equal("BenchmarkRun", GetLogFileName(artifacts).Split('-')[0]);
        }

        [Fact]
        public void ArtifactsAreNamedAfterTheCustomTitle()
        {
            using var artifacts = new TempFolder();

            RunBenchmarks(artifacts, CustomTitle, join: false, typeof(FirstBenchmarks));

            Assert.Equal([$"{CustomTitle}-report-github.md"], GetExportedFiles(artifacts));
            Assert.StartsWith($"{CustomTitle}-", GetLogFileName(artifacts));
        }

        [Fact] // the title is defined by the config, so all the summaries of the run would otherwise share it
        public void TypeNameIsAppendedToTheCustomTitleWhenTheRunReportsManySummaries()
        {
            using var artifacts = new TempFolder();

            RunBenchmarks(artifacts, CustomTitle, join: false, typeof(FirstBenchmarks), typeof(SecondBenchmarks));

            Assert.Equal(
                [$"{CustomTitle}-{FirstTypeName}-report-github.md", $"{CustomTitle}-{SecondTypeName}-report-github.md"],
                GetExportedFiles(artifacts));
        }

        [Fact]
        public void JoinedResultsAreExportedToASingleFileNamedAfterTheCustomTitle()
        {
            using var artifacts = new TempFolder();

            RunBenchmarks(artifacts, CustomTitle, join: true, typeof(FirstBenchmarks), typeof(SecondBenchmarks));

            Assert.Equal([$"{CustomTitle}-report-github.md"], GetExportedFiles(artifacts));
        }

        [Fact]
        public void JoinedResultsFallBackToADefaultFileNameWhenNoTitleIsSet()
        {
            using var artifacts = new TempFolder();

            RunBenchmarks(artifacts, title: null, join: true, typeof(FirstBenchmarks), typeof(SecondBenchmarks));

            Assert.Equal(["BenchmarkRun-joined-report-github.md"], GetExportedFiles(artifacts));
        }

        private void RunBenchmarks(TempFolder artifacts, string? title, bool join, params Type[] types)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default))
                .AddExporter(MarkdownExporter.GitHub)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddLogger(new OutputLogger(Output))
                .WithArtifactsPath(artifacts.Path);

            if (join)
                config = config.WithOptions(ConfigOptions.JoinSummary);
            if (title != null)
                config = config.WithTitle(title);

            var summaries = BenchmarkRunner.Run(types, config);

            Assert.All(summaries, summary => Assert.False(summary.HasCriticalValidationErrors));
        }

        private static string[] GetExportedFiles(TempFolder artifacts)
            => Directory.GetFiles(Path.Combine(artifacts.Path, "results")).Select(Path.GetFileName).OrderBy(name => name).ToArray()!;

        private static string GetLogFileName(TempFolder artifacts)
            => Path.GetFileNameWithoutExtension(Directory.GetFiles(artifacts.Path, "*.log").Single());

        private sealed class TempFolder : IDisposable
        {
            internal string Path { get; } = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName())).FullName;

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch (IOException) { } // the artifacts are not worth failing the test over
            }
        }
    }

    public class FirstBenchmarks
    {
        [Benchmark]
        public void Method() { }
    }

    public class SecondBenchmarks
    {
        [Benchmark]
        public void Method() { }
    }
}
