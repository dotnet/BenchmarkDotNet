using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests.Running
{
    public class BenchmarkConverterTests
    {
        /// <summary>
        /// https://github.com/dotnet/BenchmarkDotNet/issues/495
        /// </summary>
        [Fact]
        public void ReadsAttributesFromBaseClass()
        {
            Benchmark benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived)).Single();
           
            Assert.NotNull(benchmark);
            Assert.NotNull(benchmark.Target);
            Assert.NotNull(benchmark.Target.IterationSetupMethod);
            Assert.NotNull(benchmark.Target.IterationCleanupMethod);
            Assert.NotNull(benchmark.Target.GlobalCleanupMethod);
            Assert.NotNull(benchmark.Target.GlobalSetupMethod);
        }

        public abstract class Base
        {
            [GlobalSetup]
            public abstract void GlobalSetup();

            [GlobalCleanup]
            public abstract void GlobalCleanup();

            [IterationSetup]
            public abstract void Setup();

            [IterationCleanup]
            public abstract void Cleanup();

            [Benchmark]
            public void Test()
            {
            }
        }

        public class Derived : Base
        {
            public override void GlobalSetup()
            {
            }

            public override void GlobalCleanup()
            {
            }

            public override void Setup()
            {
            }

            public override void Cleanup()
            {
            }
        }
    }
}
