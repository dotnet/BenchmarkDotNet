using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [DryJob]
    [CategoriesColumn]
    [BenchmarkCategory("ClassCategory")]
    public class IntroCategories
    {
        [Benchmark]
        [BenchmarkCategory("CategoryA")]
        public void A() => Thread.Sleep(10);

        [Benchmark]
        [BenchmarkCategory("CategoryB")]
        public void B() => Thread.Sleep(10);
    }
}