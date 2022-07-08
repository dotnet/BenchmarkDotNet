using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
{
    public class ImmutableConfigTests
    {
        [Fact]
        public void DuplicateColumnProvidersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddColumnProvider(DefaultColumnProviders.Job);
            mutable.AddColumnProvider(DefaultColumnProviders.Job);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(DefaultColumnProviders.Job, final.GetColumnProviders().Single());
        }

        [Fact]
        public void DuplicateLoggersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddLogger(ConsoleLogger.Default);
            mutable.AddLogger(ConsoleLogger.Default);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(ConsoleLogger.Default, final.GetLoggers().Single());
        }

        [Fact]
        public void DuplicateHardwareCountersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddHardwareCounters(HardwareCounter.CacheMisses);
            mutable.AddHardwareCounters(HardwareCounter.CacheMisses);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Equal(HardwareCounter.CacheMisses, final.GetHardwareCounters().Single());
        }

        [FactClassicDotNetOnly(skipReason: "We have hardware counters diagnosers only for Windows. This test is disabled for .NET Core because NativeAOT compiler goes crazy when some dependency has reference to TraceEvent...")]
        public void WhenUserDefinesHardwareCountersWeChooseTheRightDiagnoser()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddHardwareCounters(HardwareCounter.CacheMisses);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(final.GetDiagnosers());
            Assert.Single(final.GetDiagnosers().OfType<IHardwareCountersDiagnoser>());
        }

        [FactClassicDotNetOnly(skipReason: "We have hardware counters diagnosers only for Windows. This test is disabled for .NET Core because NativeAOT compiler goes crazy when some dependency has reference to TraceEvent...")]
        public void WhenUserDefinesHardwareCountersAndUsesDisassemblyDiagnoserWeAddInstructionPointerExporter()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddHardwareCounters(HardwareCounter.CacheMisses);
            mutable.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()));

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(final.GetDiagnosers().OfType<IHardwareCountersDiagnoser>());
            Assert.Single(final.GetDiagnosers().OfType<DisassemblyDiagnoser>());
            Assert.Single(final.GetExporters().OfType<InstructionPointerExporter>());
        }

        [Fact]
        public void DuplicateDiagnosersAreExcludedBasedOnType()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()));
            mutable.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()));

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(final.GetDiagnosers());
        }

        [Fact]
        public void DuplicateExportersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddExporter(MarkdownExporter.GitHub);
            mutable.AddExporter(MarkdownExporter.GitHub);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(MarkdownExporter.GitHub, final.GetExporters().Single());
        }

        [Fact]
        public void DuplicateAnalyzersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddAnalyser(OutliersAnalyser.Default);
            mutable.AddAnalyser(OutliersAnalyser.Default);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(OutliersAnalyser.Default, final.GetAnalysers().Single());
        }

        [Fact]
        public void DuplicateValidatorsAreExcludedBasedOnTreatsWarningsAsErrorsProperty()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddValidator(JitOptimizationsValidator.DontFailOnError);
            mutable.AddValidator(JitOptimizationsValidator.FailOnError);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(JitOptimizationsValidator.FailOnError, final.GetValidators().OfType<JitOptimizationsValidator>().Single());
        }

        [Fact]
        public void BaseLineValidatorIsMandatory()
        {
            var fromEmpty = ImmutableConfigBuilder.Create(ManualConfig.CreateEmpty());

            Assert.Contains(BaselineValidator.FailOnError, fromEmpty.GetValidators());
        }

        [Fact]
        public void JitOptimizationsValidatorIsMandatoryByDefault()
        {
            var fromEmpty = ImmutableConfigBuilder.Create(ManualConfig.CreateEmpty());
            Assert.Contains(JitOptimizationsValidator.DontFailOnError, fromEmpty.GetValidators());

#if !DEBUG
            // DefaultConfig.Instance doesn't include JitOptimizationsValidator.FailOnError in the DEBUG mode
            var fromDefault = ImmutableConfigBuilder.Create(DefaultConfig.Instance);
            Assert.Contains(JitOptimizationsValidator.FailOnError, fromDefault.GetValidators());
#endif
        }

        [Fact]
        public void JitOptimizationsValidatorIsMandatoryCanBeDisabledOnDemand()
        {
            var disabled = ImmutableConfigBuilder.Create(ManualConfig.CreateEmpty().WithOptions(ConfigOptions.DisableOptimizationsValidator));

            Assert.DoesNotContain(JitOptimizationsValidator.FailOnError, disabled.GetValidators());
            Assert.DoesNotContain(JitOptimizationsValidator.DontFailOnError, disabled.GetValidators());

            var enabledThenDisabled = ImmutableConfigBuilder.Create(ManualConfig.CreateEmpty()
                .AddValidator(JitOptimizationsValidator.FailOnError) // we enable it first (to mimic few configs merge)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)); // then disable

            Assert.DoesNotContain(JitOptimizationsValidator.FailOnError, enabledThenDisabled.GetValidators());
            Assert.DoesNotContain(JitOptimizationsValidator.DontFailOnError, enabledThenDisabled.GetValidators());
        }

        [Fact] // See https://github.com/dotnet/BenchmarkDotNet/issues/172
        public void MissingExporterDependencyIsAddedWhenNeeded()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddExporter(TestExporter.Default);

            var exporters = ImmutableConfigBuilder.Create(mutable).GetExporters().ToArray();

            Assert.Equal(2, exporters.Length);
            Assert.Equal(new IExporter[] { TestExporterDependency.Default, TestExporter.Default }, exporters);
        }

        [Fact]
        public void MissingDependencyIsNotAddedWhenItIsAlreadyPresent()
        {
            var mutable = ManualConfig.CreateEmpty();

            mutable.AddExporter(TestExporter.Default);
            mutable.AddExporter(TestExporterDependency.Default);

            var exporters = ImmutableConfigBuilder.Create(mutable).GetExporters().ToArray();

            Assert.Equal(2, exporters.Length);
            Assert.Equal(new IExporter[] { TestExporterDependency.Default, TestExporter.Default }, exporters);
        }

        [Fact]
        public void WhenTwoConfigsAreAddedTheRegularJobsAreJustAdded()
        {
            var configWithClrJob = CreateConfigFromJobs(Job.Default.WithRuntime(CoreRuntime.Core21));
            var configWithCoreJob = CreateConfigFromJobs(Job.Default.WithRuntime(ClrRuntime.Net462));

            foreach (var added in AddLeftToTheRightAndRightToTheLef(configWithClrJob, configWithCoreJob))
            {
                var runnableJobs = added.GetJobs();

                Assert.Equal(2, runnableJobs.Count());
                Assert.Single(runnableJobs, job => job.Environment.Runtime is ClrRuntime);
                Assert.Single(runnableJobs, job => job.Environment.Runtime is CoreRuntime);
            }
        }

        [Fact]
        public void WhenTwoConfigsAreAddedTheMutatorJobsAreAppliedToAllOtherJobs()
        {
            const int warmupCount = 2;
            var configWithMutatorJob = CreateConfigFromJobs(Job.Default.WithWarmupCount(warmupCount).AsMutator());
            var configWithTwoStandardJobs = CreateConfigFromJobs(
                Job.Default.WithRuntime(ClrRuntime.Net462),
                Job.Default.WithRuntime(CoreRuntime.Core21));

            foreach (var added in AddLeftToTheRightAndRightToTheLef(configWithTwoStandardJobs, configWithMutatorJob))
            {
                var runnableJobs = added.GetJobs();

                Assert.Equal(2, runnableJobs.Count());
                Assert.All(runnableJobs, job => Assert.Equal(warmupCount, job.Run.WarmupCount));
                Assert.Single(runnableJobs, job => job.Environment.Runtime is ClrRuntime);
                Assert.Single(runnableJobs, job => job.Environment.Runtime is CoreRuntime);
            }
        }

        [Fact]
        public void WhenTwoConfigsAreAddedTheMutatorJobsAreAppliedToCustomDefaultJobIfPresent()
        {
            const int warmupCount = 2;
            const int iterationsCount = 10;

            var configWithCustomDefaultJob = CreateConfigFromJobs(Job.Default.WithIterationCount(iterationsCount).AsDefault());
            var configWithMutatorJob = CreateConfigFromJobs(Job.Default.WithWarmupCount(warmupCount).AsMutator());

            foreach (var added in AddLeftToTheRightAndRightToTheLef(configWithCustomDefaultJob, configWithMutatorJob))
            {
                var mergedJob = added.GetJobs().Single();
                Assert.Equal(warmupCount, mergedJob.Run.WarmupCount);
                Assert.Equal(iterationsCount, mergedJob.Run.IterationCount);
                Assert.False(mergedJob.Meta.IsMutator); // after the merge the "child" job becomes a standard job
            }
        }

        [Fact]
        public void WhenTwoConfigsAreAddedTheMutatorJobsAreAppliedToDefaultJobIfCustomDefaultJobIsNotPresent()
        {
            const int warmupCount = 2;
            var configWithMutatorJob = CreateConfigFromJobs(Job.Default.WithWarmupCount(warmupCount).AsMutator());

            foreach (var added in AddLeftToTheRightAndRightToTheLef(ManualConfig.CreateEmpty(), configWithMutatorJob))
            {
                var mergedJob = added.GetJobs().Single();
                Assert.Equal(warmupCount, mergedJob.Run.WarmupCount);
                Assert.False(mergedJob.Meta.IsDefault); // after the merge the "child" job becomes a standard job
                Assert.False(mergedJob.Meta.IsMutator); // after the merge the "child" job becomes a standard job
                Assert.Single(mergedJob.GetCharacteristicsWithValues(), changedCharacteristic => ReferenceEquals(changedCharacteristic, Jobs.RunMode.WarmupCountCharacteristic));
            }
        }

        [Fact]
        public void WhenArtifactsPathIsNullDefaultValueShouldBeUsed()
        {
            var mutable = ManualConfig.CreateEmpty();
            var final = ImmutableConfigBuilder.Create(mutable);
            Assert.Equal(final.ArtifactsPath, DefaultConfig.Instance.ArtifactsPath);
        }

        [Fact]
        public void WhenOrdererIsNullDefaultValueShouldBeUsed()
        {
            var mutable = ManualConfig.CreateEmpty();
            var final = ImmutableConfigBuilder.Create(mutable);
            Assert.Equal(final.Orderer, DefaultOrderer.Instance);
        }

        [Fact]
        public void WhenSummaryStyleIsNullDefaultValueShouldBeUsed()
        {
            var mutable = ManualConfig.CreateEmpty();
            var final = ImmutableConfigBuilder.Create(mutable);
            Assert.Equal(final.SummaryStyle, SummaryStyle.Default);
        }

        [Fact]
        public void WhenTimeoutIsNotSpecifiedTheDefaultValueIsUsed()
        {
            var mutable = ManualConfig.CreateEmpty();
            var final = ImmutableConfigBuilder.Create(mutable);
            Assert.Equal(DefaultConfig.Instance.BuildTimeout, final.BuildTimeout);
        }

        [Fact]
        public void CustomTimeoutHasPrecedenceOverDefaultTimeout()
        {
            TimeSpan customTimeout = TimeSpan.FromSeconds(1);
            var mutable = ManualConfig.CreateEmpty().WithBuildTimeout(customTimeout);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Equal(customTimeout, final.BuildTimeout);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenTwoCustomTimeoutsAreProvidedTheLongerOneIsUsed(bool direction)
        {
            var oneSecond = ManualConfig.CreateEmpty().WithBuildTimeout(TimeSpan.FromSeconds(1));
            var twoSeconds = ManualConfig.CreateEmpty().WithBuildTimeout(TimeSpan.FromSeconds(2));

            if (direction)
                oneSecond.Add(twoSeconds);
            else
                twoSeconds.Add(oneSecond);

            var final = ImmutableConfigBuilder.Create(direction ? oneSecond : twoSeconds);
            Assert.Equal(TimeSpan.FromSeconds(2), final.BuildTimeout);
        }

        private static ManualConfig CreateConfigFromJobs(params Job[] jobs)
        {
            var config = ManualConfig.CreateEmpty();

            config.AddJob(jobs);

            return config;
        }

        private static ImmutableConfig[] AddLeftToTheRightAndRightToTheLef(ManualConfig left, ManualConfig right)
        {
            var rightAddedToLeft = ManualConfig.Create(left);
            rightAddedToLeft.Add(right);

            var leftAddedToTheRight = ManualConfig.Create(right);
            leftAddedToTheRight.Add(left);

            return new[]{ rightAddedToLeft.CreateImmutableConfig(), leftAddedToTheRight.CreateImmutableConfig() };
        }

        public class TestExporter : IExporter, IExporterDependencies
        {
            public static readonly TestExporter Default = new TestExporter();

            public IEnumerable<IExporter> Dependencies
            {
                get { yield return TestExporterDependency.Default; }
            }

            public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) => Enumerable.Empty<string>();

            public string Name => nameof(TestExporter);
            public void ExportToLog(Summary summary, ILogger logger) { }
        }

        public class TestExporterDependency : IExporter
        {
            public static readonly TestExporterDependency Default = new TestExporterDependency();

            public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) => Enumerable.Empty<string>();

            public string Name => nameof(TestExporterDependency);
            public void ExportToLog(Summary summary, ILogger logger) { }
        }

        [Fact]
        public void GenerateWarningWhenExporterDependencyAlreadyExistInConfig()
        {
            System.Globalization.CultureInfo currentCulture = default;
            System.Globalization.CultureInfo currentUICulture = default;
            {
                var ct = System.Threading.Thread.CurrentThread;
                currentCulture = ct.CurrentCulture;
                currentUICulture = ct.CurrentUICulture;
                ct.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                ct.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            }
            try
            {
                var mutable = ManualConfig.CreateEmpty();
                mutable.AddExporter(new BenchmarkDotNet.Exporters.Csv.CsvMeasurementsExporter(BenchmarkDotNet.Exporters.Csv.CsvSeparator.Comma));
                mutable.AddExporter(RPlotExporter.Default);

                var final = ImmutableConfigBuilder.Create(mutable);

                Assert.Equal(1, final.ConfigAnalysisConclusion.Count);
            }
            finally
            {
                var ct = System.Threading.Thread.CurrentThread;
                ct.CurrentCulture = currentCulture;
                ct.CurrentUICulture = currentUICulture;

            }

        }
    }
}
