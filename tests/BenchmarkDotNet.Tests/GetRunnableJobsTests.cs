using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class GetRunnableJobsTests
    {
        [Fact]
        public void WhenTwoConfigsAreAddedTheRegularJobsAreJustAdded()
        {
            var configWithClrJob = CreateConfigFromJobs(Job.Clr);
            var cofingWithCoreJob = CreateConfigFromJobs(Job.Core);

            foreach (var added in AddLeftToTheRightAndRightToTheLef(configWithClrJob, cofingWithCoreJob))
            {
                var runnableJobs = added.GetRunnableJobs();
                
                Assert.Equal(2, runnableJobs.Count);
                Assert.Single(runnableJobs, job => job.Environment.Runtime is ClrRuntime);
                Assert.Single(runnableJobs, job => job.Environment.Runtime is CoreRuntime);
            }
        }
        
        [Fact]
        public void WhenTwoConfigsAreAddedTheMutatorJobsAreAppliedToAllOtherJobs()
        {
            const int warmupCount = 2;
            var configWithMutatorJob = CreateConfigFromJobs(Job.Default.WithWarmupCount(warmupCount).AsMutator());
            var configWithTwoStandardJobs = CreateConfigFromJobs(Job.Clr, Job.Core);

            foreach (var added in AddLeftToTheRightAndRightToTheLef(configWithTwoStandardJobs, configWithMutatorJob))
            {
                var runnableJobs = added.GetRunnableJobs();
                
                Assert.Equal(2, runnableJobs.Count);
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
                var mergedJob = added.GetRunnableJobs().Single();
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
                var mergedJob = added.GetRunnableJobs().Single();
                Assert.Equal(warmupCount, mergedJob.Run.WarmupCount);
                Assert.False(mergedJob.Meta.IsDefault); // after the merge the "child" job becomes a standard job
                Assert.False(mergedJob.Meta.IsMutator); // after the merge the "child" job becomes a standard job
                Assert.Single(mergedJob.GetCharacteristicsWithValues(), changedCharacteristic => ReferenceEquals(changedCharacteristic, RunMode.WarmupCountCharacteristic));
            }
        }
        
        private static ManualConfig CreateConfigFromJobs(params Job[] jobs)
        {
            var config = ManualConfig.CreateEmpty();
            
            config.Add(jobs);

            return config;
        }
        
        private static ManualConfig[] AddLeftToTheRightAndRightToTheLef(ManualConfig left, ManualConfig right)
        {
            var rightAddedToLeft = ManualConfig.Create(left);
            rightAddedToLeft.Add(right);
            
            var leftAddedToTheRight = ManualConfig.Create(right);
            leftAddedToTheRight.Add(left);

            return new []{ rightAddedToLeft, leftAddedToTheRight };
        }
    }
}