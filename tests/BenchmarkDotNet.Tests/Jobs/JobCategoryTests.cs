using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
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

        // Applying a mutator can make two jobs identical, so they are deduplicated once more afterwards. That pass
        // has to merge the categories as well, otherwise the ones carried only by the jobs it drops are lost and
        // selecting by them matches nothing.
        [Fact]
        public void CategoriesSurviveTheDeduplicationDoneAfterTheMutatorsAreApplied()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Default.WithWarmupCount(1).WithCategory("first"))
                .AddJob(Job.Default.WithWarmupCount(3).WithCategory("second"))
                .AddJob(Job.Default.WithWarmupCount(5).AsMutator());

            var job = Assert.Single(ImmutableConfigBuilder.Create(config).GetJobs());

            Assert.Equal(5, job.Run.WarmupCount);
            Assert.True(job.Meta.HasCategory("first"));
            Assert.True(job.Meta.HasCategory("second"));
        }

        [Fact]
        public void JobsWhichDifferOnlyByCategoriesAreDeduplicatedIntoOne()
        {
            // Categories are not a part of a job's identity: keeping both would run the same job twice and produce
            // two summary rows with the same name.
            var mutable = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithCategory("net8"))
                .AddJob(Job.Dry.WithCategory("net9"));

            var job = Assert.Single(ImmutableConfigBuilder.Create(mutable).GetJobs());

            // The survivor carries the categories of all of them, so selecting by either one still matches.
            Assert.True(job.Meta.HasCategory("net8"));
            Assert.True(job.Meta.HasCategory("net9"));
        }

        // Apply overwrites the characteristics, but the categories are additive, so ImmutableConfigBuilder merges
        // them explicitly: a mutator that carries categories must not wipe the categories of the job it mutates
        [Fact]
        public void AMutatorAddsItsCategoriesToTheJobsItMutates()
        {
            const int warmupCount = 2;
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithCategory("keep"))
                .AddJob(Job.Default.WithWarmupCount(warmupCount).WithCategory("mutator").AsMutator());

            var job = Assert.Single(ImmutableConfigBuilder.Create(config).GetJobs());

            Assert.Equal(warmupCount, job.Run.WarmupCount); // the mutator was applied
            Assert.True(job.Meta.HasCategory("keep"));      // ...without dropping what the job already had
            Assert.True(job.Meta.HasCategory("mutator"));
        }

        [Fact]
        public void AMutatorWithoutCategoriesLeavesTheCategoriesOfTheJobsItMutates()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithCategory("keep"))
                .AddJob(Job.Default.WithWarmupCount(2).AsMutator());

            var job = Assert.Single(ImmutableConfigBuilder.Create(config).GetJobs());

            Assert.Equal(["keep"], job.Meta.Categories);
        }

        // The mutator jobs are deduplicated like any other, and merging their categories must not cost them their
        // IsMutator flag: WithCategories copies the job through Apply, which skips the characteristics that are
        // ignored on apply. A mutator which loses the flag is added to the config as an extra standalone job and is
        // never applied to the jobs it was meant to mutate.
        [Fact]
        public void DeduplicatedMutatorsWhichCarryDifferentCategoriesAreStillMutators()
        {
            const int warmupCount = 2;
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithId("base"))
                .AddJob(Job.Default.WithWarmupCount(warmupCount).WithCategory("first").AsMutator())
                .AddJob(Job.Default.WithWarmupCount(warmupCount).WithCategory("second").AsMutator());

            var job = Assert.Single(ImmutableConfigBuilder.Create(config).GetJobs());

            Assert.Equal("base", job.Id);
            Assert.Equal(warmupCount, job.Run.WarmupCount);
            Assert.True(job.Meta.HasCategory("first"));
            Assert.True(job.Meta.HasCategory("second"));
        }

        // The categories of a group are deduplicated before they are compared to the ones the survivor already has,
        // so a group whose jobs carry the very same categories is recognized as having nothing to merge.
        [Fact]
        public void DeduplicatedMutatorsWhichCarryTheSameCategoriesAreStillMutators()
        {
            const int warmupCount = 2;
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithId("base"))
                .AddJob(Job.Default.WithWarmupCount(warmupCount).WithCategory("mutator").AsMutator())
                .AddJob(Job.Default.WithWarmupCount(warmupCount).WithCategory("mutator").AsMutator());

            var job = Assert.Single(ImmutableConfigBuilder.Create(config).GetJobs());

            Assert.Equal("base", job.Id);
            Assert.Equal(warmupCount, job.Run.WarmupCount);
            Assert.Equal(["mutator"], job.Meta.Categories);
        }

        // UnfreezeCopy is built on Apply, so marking the characteristic as ignored on apply (which would be the
        // obvious way to make the categories additive) would silently drop them on every WithXxx call
        [Fact]
        public void CategoriesSurviveCopyingTheJob()
        {
            Assert.Equal(["net8"], Job.Dry.WithCategory("net8").UnfreezeCopy().Meta.Categories);
            Assert.Equal(["net8", "slow"], Job.Dry.WithCategory("net8").WithLaunchCount(2).WithCategory("slow").Meta.Categories);
        }

        [Fact]
        public void CategoriesAreNotComparedWhenJobsAreOrdered()
        {
            Assert.Equal(0, JobComparer.Default.Compare(Job.Dry.WithCategory("a"), Job.Dry.WithCategory("b")));
            Assert.Equal(0, JobComparer.Default.Compare(Job.Dry, Job.Dry.WithCategory("a")));
        }

        [Fact]
        public void AnEmptySetOfCategoriesLeavesTheJobUnchanged()
        {
            var job = Job.Default.WithCategories();

            // Setting the characteristic to an empty list would mark the job as changed, so a `WithCategories(list)`
            // where the list happens to be empty would run the benchmark a second time.
            Assert.Empty(job.Meta.Categories);
            Assert.False(job.HasValue(MetaMode.CategoriesCharacteristic));

            var config = ManualConfig.CreateEmpty().AddJob(Job.Default).AddJob(Job.Default.WithCategories());
            Assert.Single(ImmutableConfigBuilder.Create(config).GetJobs());
        }

        [Fact]
        public void AnEmptySetOfCategoriesClearsTheCategoriesTheJobAlreadyHad()
        {
            Assert.Empty(Job.Dry.WithCategory("a").WithCategories().Meta.Categories);
        }

        // the guard lives in MetaMode rather than in the WithCategory/WithCategories extensions, because the
        // property and AddCategories are public too and would otherwise report a null `source` out of Distinct
        [Fact]
        public void ANullSetOfCategoriesIsRejectedWhicheverWayItIsPassed()
        {
            var job = new Job();

            Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => Job.Default.WithCategories(null!)).ParamName);
            Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => job.Meta.Categories = null!).ParamName);
            Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => job.Meta.AddCategories(null!)).ParamName);
        }

        // a null category matches nothing, survives the merging done when jobs are deduplicated, and only fails
        // once something formats it, so it is rejected where it is introduced
        [Fact]
        public void ANullCategoryIsRejected()
        {
            Assert.Equal("categories", Assert.Throws<ArgumentException>(() => Job.Default.WithCategory(null!)).ParamName);
            Assert.Equal("categories", Assert.Throws<ArgumentException>(() => Job.Default.WithCategories("first", null!)).ParamName);
            Assert.Equal("categories", Assert.Throws<ArgumentException>(() => new Job().Meta.AddCategories([null!])).ParamName);
        }

        [Fact]
        public void EachJobAttributeCarriesItsOwnCategories()
        {
            var jobs = BenchmarkConverter.TypeToBenchmarks(typeof(WithTwoCategorizedJobs))
                .BenchmarksCases
                .Select(benchmarkCase => benchmarkCase.Job)
                .ToDictionary(job => job.ResolvedId);

            Assert.Equal(2, jobs.Count);

            Assert.True(jobs["first"].Meta.HasCategory("runtimes"));
            Assert.True(jobs["first"].Meta.HasCategory("net8"));
            Assert.False(jobs["first"].Meta.HasCategory("net9"));

            Assert.True(jobs["second"].Meta.HasCategory("runtimes"));
            Assert.True(jobs["second"].Meta.HasCategory("net9"));
            Assert.False(jobs["second"].Meta.HasCategory("net8"));
        }

        [Fact]
        public void CategoriesAreAvailableOnTheAttributesWhichDefineAJob()
        {
            var job = BenchmarkConverter.TypeToBenchmarks(typeof(WithACategorizedDryJob)).BenchmarksCases.Single().Job;

            Assert.True(job.Meta.HasCategory("debug"));
        }

        [Fact]
        public void AJobAttributeWithoutCategoriesProducesAJobWithoutCategories()
        {
            var job = BenchmarkConverter.TypeToBenchmarks(typeof(WithAnUncategorizedJob)).BenchmarksCases.Single().Job;

            Assert.Empty(job.Meta.Categories);
        }

        [Fact]
        public void ANullCategoriesArgumentIsTreatedAsNoCategories()
        {
            // `Categories = null` is legal C# for an attribute argument of an array type, so discovery must not
            // throw a NullReferenceException on it.
            var job = BenchmarkConverter.TypeToBenchmarks(typeof(WithNullCategories)).BenchmarksCases.Single().Job;

            Assert.Empty(job.Meta.Categories);
        }

        [Fact]
        public void TheCategoriesOfAJobAttributeAreNotPartOfItsId()
        {
            var categorized = BenchmarkConverter.TypeToBenchmarks(typeof(WithACategorizedDryJob)).BenchmarksCases.Single().Job;
            var uncategorized = BenchmarkConverter.TypeToBenchmarks(typeof(WithAnUncategorizedJob)).BenchmarksCases.Single().Job;

            Assert.Equal(uncategorized.ResolvedId, categorized.ResolvedId);
        }

        [Fact]
        public void TheFilterSelectsBenchmarksByTheCategoriesOfAJobAttribute()
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(WithTwoCategorizedJobs)).BenchmarksCases;

            var matched = benchmarkCases
                .Where(benchmarkCase => new JobCategoryFilter(["net9"]).Predicate(benchmarkCase))
                .ToArray();

            Assert.Equal("second", Assert.Single(matched).Job.ResolvedId);
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

        // every category filter is inclusion only, so a job without categories belongs to none of the requested
        // ones. This is the documented behaviour: a config mixing categorized and uncategorized jobs loses the
        // uncategorized ones as soon as a category is selected.
        [Fact]
        public void TheFilterExcludesJobsWithoutCategories()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithId("net8").WithCategory("net8"))
                .AddJob(Job.Dry.WithId("baseline"));

            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(WithSingleBenchmark), config).BenchmarksCases;
            var filter = new JobCategoryFilter(["net8"]);

            Assert.Equal("net8", Assert.Single(benchmarkCases, filter.Predicate).Job.ResolvedId);
        }

        [Fact]
        public void TheFilterIsAvailableAsAnAttribute()
        {
            var benchmarkCase = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(WithAFilteredJobCategory)).BenchmarksCases);

            Assert.Equal("second", benchmarkCase.Job.ResolvedId);
        }

        [Fact]
        public void TheFilterIsAvailableAsAConsoleArgument()
        {
            var (isSuccess, config, options) = ConfigParser.Parse(["--jobCategories", "net8", "runtimes"], NullLogger.Instance);

            Assert.True(isSuccess);
            Assert.True(options!.UserProvidedFilters);

            var filter = Assert.Single(config!.GetFilters().OfType<JobCategoryFilter>());
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(WithTwoCategorizedJobs)).BenchmarksCases;

            Assert.Equal(2, benchmarkCases.Count(filter.Predicate)); // both jobs are in "runtimes"
        }

        public class WithSingleBenchmark
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 1, id: "first", Categories = ["net8"])]
        [SimpleJob(launchCount: 2, warmupCount: 1, iterationCount: 1, id: "second", Categories = ["net9"])]
        [JobCategoryFilter("net9")]
        public class WithAFilteredJobCategory
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 1, id: "first", Categories = ["runtimes", "net8"])]
        [SimpleJob(launchCount: 2, warmupCount: 1, iterationCount: 1, id: "second", Categories = ["runtimes", "net9"])]
        public class WithTwoCategorizedJobs
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [DryJob(Categories = ["debug"])]
        public class WithACategorizedDryJob
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [DryJob]
        public class WithAnUncategorizedJob
        {
            [Benchmark] public void TheBenchmark() { }
        }

        [DryJob(Categories = null!)]
        public class WithNullCategories
        {
            [Benchmark] public void TheBenchmark() { }
        }
    }
}
