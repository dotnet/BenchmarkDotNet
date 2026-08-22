using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests.Jobs
{
    public class JobCategoryTests
    {
        [Fact]
        public void JobsHaveNoCategoriesByDefault()
        {
            Assert.Empty(Job.Default.Meta.Categories);
            Assert.False(Job.Default.Meta.HasCategory("anything"));
        }

        [Fact]
        public void WithCategoriesOverridesTheExistingCategories()
        {
            var job = Job.Dry.WithCategories("first").WithCategories("second", "third");

            Assert.Equal(["second", "third"], job.Meta.Categories);
        }

        [Fact]
        public void WithCategoryAddsToTheExistingCategories()
        {
            var job = Job.Dry.WithCategories("first").WithCategory("second");

            Assert.Equal(["first", "second"], job.Meta.Categories);
        }

        [Fact]
        public void TheSameCategoryIsNotAddedTwice()
        {
            var job = Job.Dry.WithCategories("dupe", "DUPE").WithCategory("Dupe");

            Assert.Equal(["dupe"], job.Meta.Categories);
        }

        [Theory]
        [InlineData("net8")]
        [InlineData("NET8")]
        [InlineData("Net8")]
        public void HasCategoryIsCaseInsensitive(string category)
        {
            Assert.True(Job.Dry.WithCategory("net8").Meta.HasCategory(category));
        }

        [Fact]
        public void HasCategoryIsAnExactMatch()
        {
            var job = Job.Dry.WithCategory("net8");

            Assert.False(job.Meta.HasCategory("net"));
            Assert.False(job.Meta.HasCategory("net80"));
        }

        [Fact]
        public void TheOriginalJobIsNotModified()
        {
            var original = Job.Dry.WithCategory("first");

            original.WithCategory("second");

            Assert.Equal(["first"], original.Meta.Categories);
        }

        // categories are metadata, they must not change the identity of the job:
        // the id is used for the folder names and the summary, and users don't expect it to change
        // just because they have categorized their jobs. See MetaMode.CategoriesCharacteristic
        [Fact]
        public void CategoriesDoNotAffectTheJobId()
        {
            var job = Job.Default.WithLaunchCount(3);
            var categorized = job.WithCategories("net8", "slow");

            Assert.Equal(job.ResolvedId, categorized.ResolvedId);
            Assert.Equal(job.FolderInfo, categorized.FolderInfo);
            Assert.Equal(job.DisplayInfo, categorized.DisplayInfo);
        }

        [Fact]
        public void CategoriesDoNotAffectTheGeneratedJobIdOfTheDefaultJob()
        {
            Assert.Equal("DefaultJob", Job.Default.WithCategory("net8").ResolvedId);
        }

        [Fact]
        public void ExplicitIdsArePreserved()
        {
            Assert.Equal("net8", Job.Dry.WithId("net8").WithCategory("runtimes").ResolvedId);
        }

        // the categories are not needed by the child process, they are used by the host to select the jobs
        [Fact]
        public void CategoriesAreNotExportedToTheGeneratedSourceCode()
        {
            var job = Job.Dry.WithLaunchCount(3);

            Assert.Equal(
                CharacteristicSetPresenter.SourceCode.ToPresentation(job),
                CharacteristicSetPresenter.SourceCode.ToPresentation(job.WithCategories("net8")));
        }

        [Fact]
        public void JobsWhichDifferOnlyByCategoriesAreNotConsideredDuplicates()
        {
            var mutable = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithCategory("net8"))
                .AddJob(Job.Dry.WithCategory("net9"));

            var final = ImmutableConfigBuilder.Create(mutable);

            Assert.Equal(2, final.GetJobs().Count());
        }

        [Fact]
        public void TheAttributeAddsItsCategoriesToEveryJob()
        {
            var jobs = BenchmarkConverter.TypeToBenchmarks(typeof(WithTwoJobsAndACategory))
                .BenchmarksCases
                .Select(benchmarkCase => benchmarkCase.Job)
                .ToArray();

            Assert.Equal(2, jobs.Length);
            Assert.All(jobs, job => Assert.True(job.Meta.HasCategory("fromAttribute")));
        }

        [Fact]
        public void TheAttributeDoesNotDropTheCategoriesDefinedInCode()
        {
            var config = ManualConfig.CreateEmpty().AddJob(Job.Dry.WithCategory("fromCode"));

            var job = BenchmarkConverter.TypeToBenchmarks(typeof(WithACategoryAttribute), config).BenchmarksCases.Single().Job;

            Assert.True(job.Meta.HasCategory("fromCode"));
            Assert.True(job.Meta.HasCategory("fromAttribute"));
        }

        [Theory]
        [InlineData("net8", 1)]
        [InlineData("NET8", 1)] // case insensitive
        [InlineData("runtimes", 2)]
        [InlineData("net", 0)] // it's an exact match, not a substring one
        [InlineData("typo", 0)]
        public void TheFilterSelectsBenchmarksByJobCategory(string category, int expectedBenchmarks)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithId("net8").WithCategories("net8", "runtimes"))
                .AddJob(Job.Dry.WithId("net9").WithCategories("net9", "runtimes"))
                .AddJob(Job.Dry.WithId("debug"));

            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(WithSingleBenchmark), config).BenchmarksCases;
            Assert.Equal(3, benchmarkCases.Length); // one per job

            var filter = new JobCategoryFilter([category]);

            Assert.Equal(expectedBenchmarks, benchmarkCases.Count(benchmarkCase => filter.Predicate(benchmarkCase)));
        }

        [Fact]
        public void MultipleFilterCategoriesAreCombinedWithOr()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithId("net8").WithCategory("net8"))
                .AddJob(Job.Dry.WithId("net9").WithCategory("net9"))
                .AddJob(Job.Dry.WithId("debug").WithCategory("debug"));

            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(WithSingleBenchmark), config).BenchmarksCases;

            var filter = new JobCategoryFilter(["net8", "net9"]);
            var matched = benchmarkCases.Where(benchmarkCase => filter.Predicate(benchmarkCase)).ToArray();

            Assert.Equal(["net8", "net9"], matched.Select(benchmarkCase => benchmarkCase.Job.ResolvedId).OrderBy(id => id));
        }

        public class WithSingleBenchmark
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [JobCategory("fromAttribute")]
        [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 1, id: "first")]
        [SimpleJob(launchCount: 2, warmupCount: 1, iterationCount: 1, id: "second")]
        public class WithTwoJobsAndACategory
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [JobCategory("fromAttribute")]
        public class WithACategoryAttribute
        {
            [Benchmark] public void TheBenchmark() { }
        }
    }
}
