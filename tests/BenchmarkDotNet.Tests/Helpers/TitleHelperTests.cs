using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests.Helpers
{
    public class TitleHelperTests
    {
        private const string CustomTitle = "MyCustomTitle";
        private const string FirstTypeName = "BenchmarkDotNet.Tests.Helpers.FirstBenchmarks";
        private const string SecondTypeName = "BenchmarkDotNet.Tests.Helpers.SecondBenchmarks";

        [Fact]
        public void SummaryTitleIsTheFullyQualifiedTypeNameWhenNoTitleIsSet()
        {
            var runInfo = CreateRunInfo(typeof(FirstBenchmarks), title: null);

            Assert.Equal(FirstTypeName, TitleHelper.GetSummaryTitle(runInfo, isTheOnlySummary: true));
        }

        [Fact]
        public void SummaryTitleEscapesTheCharactersThatAreInvalidForAPath()
        {
            var runInfo = CreateRunInfo(typeof(Generic<int>), title: null);

            Assert.Equal("BenchmarkDotNet.Tests.Helpers.Generic_Int32_", TitleHelper.GetSummaryTitle(runInfo, isTheOnlySummary: true));
        }

        [Fact]
        public void SummaryTitleIsTheCustomTitleWhenTheRunReportsASingleSummary()
        {
            var runInfo = CreateRunInfo(typeof(FirstBenchmarks), CustomTitle);

            Assert.Equal(CustomTitle, TitleHelper.GetSummaryTitle(runInfo, isTheOnlySummary: true));
        }

        [Fact] // the custom title is shared by all the summaries, so their exported files must not be named after it alone #529
        public void SummaryTitleCombinesTheCustomTitleWithTheTypeNameWhenTheRunReportsManySummaries()
        {
            var first = CreateRunInfo(typeof(FirstBenchmarks), CustomTitle);
            var second = CreateRunInfo(typeof(SecondBenchmarks), CustomTitle);

            Assert.Equal($"{CustomTitle}-{FirstTypeName}", TitleHelper.GetSummaryTitle(first, isTheOnlySummary: false));
            Assert.Equal($"{CustomTitle}-{SecondTypeName}", TitleHelper.GetSummaryTitle(second, isTheOnlySummary: false));
        }

        [Theory]
        [InlineData("with/slash", "with_slash")]
        [InlineData("with:colon", "with_colon")]
        public void CustomTitleIsEscaped(string title, string expected)
        {
            var runInfo = CreateRunInfo(typeof(FirstBenchmarks), title);

            Assert.Equal(expected, TitleHelper.GetSummaryTitle(runInfo, isTheOnlySummary: true));
        }

        [Fact]
        public void TooLongTitleIsTruncated()
        {
            var runInfo = CreateRunInfo(typeof(FirstBenchmarks), title: null);

            var title = TitleHelper.GetSummaryTitle(runInfo, isTheOnlySummary: true, desiredMaxLength: 10);

            Assert.Equal(10, title.Length);
            Assert.StartsWith("Bench", title);
            Assert.EndsWith("arks", title);
        }

        [Fact]
        public void RunTitleIsTheTypeNameWhenAllTheBenchmarksBelongToASingleType()
        {
            var runInfos = new[] { CreateRunInfo(typeof(FirstBenchmarks), title: null) };

            Assert.Equal(FirstTypeName, TitleHelper.GetRunTitle(runInfos));
        }

        [Fact]
        public void RunTitleIsSharedByAllTheTypesWhenThereIsMoreThanOne()
        {
            var runInfos = new[] { CreateRunInfo(typeof(FirstBenchmarks), title: null), CreateRunInfo(typeof(SecondBenchmarks), title: null) };

            Assert.Equal(TitleHelper.MultipleTypesTitle, TitleHelper.GetRunTitle(runInfos));
        }

        [Fact]
        public void RunTitleIsTheCustomTitleWhenItIsSet()
        {
            var runInfos = new[] { CreateRunInfo(typeof(FirstBenchmarks), CustomTitle), CreateRunInfo(typeof(SecondBenchmarks), CustomTitle) };

            Assert.Equal(CustomTitle, TitleHelper.GetRunTitle(runInfos));
        }

        [Fact]
        public void JoinedSummaryTitleFallsBackToTheDefaultWhenNoTitleIsSet()
        {
            Assert.Equal(TitleHelper.JoinedSummaryTitle, TitleHelper.GetJoinedSummaryTitle(customTitle: null));
            Assert.Equal(TitleHelper.JoinedSummaryTitle, TitleHelper.GetJoinedSummaryTitle(customTitle: " "));
        }

        [Fact]
        public void JoinedSummaryTitleIsTheCustomTitleWhenItIsSet()
        {
            Assert.Equal(CustomTitle, TitleHelper.GetJoinedSummaryTitle(CustomTitle));
        }

        private static BenchmarkRunInfo CreateRunInfo(Type type, string? title)
            => BenchmarkConverter.TypeToBenchmarks(type, title == null ? ManualConfig.CreateEmpty() : ManualConfig.CreateEmpty().WithTitle(title));
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

    public class Generic<T>
    {
        [Benchmark]
        public void Method() { }
    }
}
