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
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ImmutableConfigTests
    {
        [Fact]
        public void DuplicateColumnProvidersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(DefaultColumnProviders.Job);
            mutable.Add(DefaultColumnProviders.Job);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(DefaultColumnProviders.Job, final.GetColumnProviders().Single());
        }

        [Fact]
        public void DuplicateLoggersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(ConsoleLogger.Default);
            mutable.Add(ConsoleLogger.Default);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(ConsoleLogger.Default, final.GetLoggers().Single());
        }

        [Fact]
        public void DuplicateHardwareCountersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(HardwareCounter.CacheMisses);
            mutable.Add(HardwareCounter.CacheMisses);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Equal(HardwareCounter.CacheMisses, final.GetHardwareCounters().Single());
        }
        
        [FactClassicDotNetOnly(skipReason: "We have hardware counters diagnosers only for Windows. This test is disabled for .NET Core because CoreRT compiler goes crazy when some dependency has reference to TraceEvent...")]
        public void WhenUserDefinesHardwareCountersWeChooseTheRightDiagnoser()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(HardwareCounter.CacheMisses);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(final.GetDiagnosers());
            Assert.Single(final.GetDiagnosers().OfType<IHardwareCountersDiagnoser>());
        }
        
        [FactClassicDotNetOnly(skipReason: "We have hardware counters diagnosers and disassembler only for Windows. This test is disabled for .NET Core because CoreRT compiler goes crazy when some dependency has reference to TraceEvent...")]
        public void WhenUserDefinesHardwareCountersAndUsesDissasemblyDiagnoserWeAddInstructionPointerExporter()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(HardwareCounter.CacheMisses);
            mutable.Add(DisassemblyDiagnoser.Create(DisassemblyDiagnoserConfig.All));

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(final.GetDiagnosers().OfType<IHardwareCountersDiagnoser>());
            Assert.Single(final.GetDiagnosers().OfType<IDisassemblyDiagnoser>());
            Assert.Single(final.GetExporters().OfType<InstructionPointerExporter>());
        }
        
        [Fact]
        public void DuplicateDiagnosersAreExcludedBasedOnType()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(DisassemblyDiagnoser.Create(DisassemblyDiagnoserConfig.All));
            mutable.Add(DisassemblyDiagnoser.Create(DisassemblyDiagnoserConfig.Asm));

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(final.GetDiagnosers());
        }

        [Fact]
        public void DuplicateExportersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(MarkdownExporter.GitHub);
            mutable.Add(MarkdownExporter.GitHub);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(MarkdownExporter.GitHub, final.GetExporters().Single());
        }
        
        [Fact]
        public void DuplicateAnalyzersAreExcluded()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(OutliersAnalyser.Default);
            mutable.Add(OutliersAnalyser.Default);

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Same(OutliersAnalyser.Default, final.GetAnalysers().Single());
        }
        
        [Fact]
        public void DuplicateValidatorsAreExcludedBasedOnTreatsWarningsAsErrorsProperty()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(JitOptimizationsValidator.DontFailOnError);
            mutable.Add(JitOptimizationsValidator.FailOnError);

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

            var fromDefault = ImmutableConfigBuilder.Create(DefaultConfig.Instance);
            Assert.Contains(JitOptimizationsValidator.FailOnError, fromDefault.GetValidators());
        }

        [Fact]
        public void JitOptimizationsValidatorIsMandatoryCanBeDisabledOnDemand()
        {
            var disabled = ImmutableConfigBuilder.Create(ManualConfig.CreateEmpty().With(ConfigOptions.DisableOptimizationsValidator));

            Assert.DoesNotContain(JitOptimizationsValidator.FailOnError, disabled.GetValidators());
            Assert.DoesNotContain(JitOptimizationsValidator.DontFailOnError, disabled.GetValidators());

            var enabledThenDisabled = ImmutableConfigBuilder.Create(ManualConfig.CreateEmpty()
                .With(JitOptimizationsValidator.FailOnError) // we enable it first (to mimic few configs merge)
                .With(ConfigOptions.DisableOptimizationsValidator)); // then disable

            Assert.DoesNotContain(JitOptimizationsValidator.FailOnError, enabledThenDisabled.GetValidators());
            Assert.DoesNotContain(JitOptimizationsValidator.DontFailOnError, enabledThenDisabled.GetValidators());
        }

        [Fact] // See https://github.com/dotnet/BenchmarkDotNet/issues/172
        public void MissingExporterDependencyIsAddedWhenNeeded()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(TestExporter.Default);

            var exporters = ImmutableConfigBuilder.Create(mutable).GetExporters().ToArray();
            
            Assert.Equal(2, exporters.Length);
            Assert.Equal(new IExporter[] { TestExporterDependency.Default, TestExporter.Default }, exporters);
        }

        [Fact]
        public void MissingDependencyIsNotAddedWhenItIsAlreadyPresent()
        {
            var mutable = ManualConfig.CreateEmpty();
            
            mutable.Add(TestExporter.Default);
            mutable.Add(TestExporterDependency.Default);
            
            var exporters = ImmutableConfigBuilder.Create(mutable).GetExporters().ToArray();
            
            Assert.Equal(2, exporters.Length);
            Assert.Equal(new IExporter[] { TestExporterDependency.Default, TestExporter.Default }, exporters);
        }
        
        [Fact]
        public void WhenTwoConfigsAreAddedTheRegularJobsAreJustAdded()
        {
            var configWithClrJob = CreateConfigFromJobs(Job.Default.With(CoreRuntime.Core21));
            var cofingWithCoreJob = CreateConfigFromJobs(Job.Default.With(ClrRuntime.Net461));

            foreach (var added in AddLeftToTheRightAndRightToTheLef(configWithClrJob, cofingWithCoreJob))
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
                Job.Default.With(ClrRuntime.Net461), 
                Job.Default.With(CoreRuntime.Core21));

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
        
        private static ManualConfig CreateConfigFromJobs(params Job[] jobs)
        {
            var config = ManualConfig.CreateEmpty();
            
            config.Add(jobs);

            return config;
        }
        
        private static ImmutableConfig[] AddLeftToTheRightAndRightToTheLef(ManualConfig left, ManualConfig right)
        {
            var rightAddedToLeft = ManualConfig.Create(left);
            rightAddedToLeft.Add(right);
            
            var leftAddedToTheRight = ManualConfig.Create(right);
            leftAddedToTheRight.Add(left);

            return new []{ rightAddedToLeft.CreateImmutableConfig(), leftAddedToTheRight.CreateImmutableConfig() };
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
    }
}