using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
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
            var derivedType = typeof(Derived);
            Benchmark benchmark = BenchmarkConverter.TypeToBenchmarks(derivedType).Benchmarks.Single();

            Assert.NotNull(benchmark);
            Assert.NotNull(benchmark.Target);

            Assert.NotNull(benchmark.Target.IterationSetupMethod);
            Assert.Equal(benchmark.Target.IterationSetupMethod.DeclaringType, derivedType);

            Assert.NotNull(benchmark.Target.IterationCleanupMethod);
            Assert.Equal(benchmark.Target.IterationCleanupMethod.DeclaringType, derivedType);

            Assert.NotNull(benchmark.Target.GlobalCleanupMethod);
            Assert.Equal(benchmark.Target.GlobalCleanupMethod.DeclaringType, derivedType);

            Assert.NotNull(benchmark.Target.GlobalSetupMethod);
            Assert.Equal(benchmark.Target.GlobalSetupMethod.DeclaringType, derivedType);
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

        [Fact]
        public void IfIterationSetupIsProvidedTheBenchmarkShouldRunOncePerIteration()
        {
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived)).Benchmarks.Single();
            
            Assert.Equal(1, benchmark.Job.Run.InvocationCount);
            Assert.Equal(1, benchmark.Job.Run.UnrollFactor);
        }
        
        [Fact]
        public void InvocationCountIsRespectedForBenchmarksWithIterationSetup()
        {
            const int InvocationCount = 100;
            
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived), 
                DefaultConfig.Instance.With(Job.Default
                    .WithInvocationCount(InvocationCount)))
                .Benchmarks.Single();
            
            Assert.Equal(InvocationCount, benchmark.Job.Run.InvocationCount);
            Assert.NotNull(benchmark.Target.IterationSetupMethod);
        }
        
        [Fact]
        public void UnrollFactorIsRespectedForBenchmarksWithIterationSetup()
        {
            const int UnrollFactor = 13;
            
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived), 
                    DefaultConfig.Instance.With(Job.Default
                        .WithUnrollFactor(UnrollFactor)))
                .Benchmarks.Single();
            
            Assert.Equal(UnrollFactor, benchmark.Job.Run.UnrollFactor);
            Assert.NotNull(benchmark.Target.IterationSetupMethod);
        }

        [Fact]
        public void JobMutatorsApplySettingsToAllNonMutatorJobs()
        {
            var info = BenchmarkConverter.TypeToBenchmarks(
                    typeof(WithMutator), 
                    DefaultConfig.Instance
                        .With(Job.Clr)
                        .With(Job.Core));
            
            Assert.Equal(2, info.Benchmarks.Length);
            Assert.All(info.Benchmarks, benchmark => Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxTargetIterationCount));
            Assert.Single(info.Benchmarks, benchmark => benchmark.Job.Env.Runtime is ClrRuntime);
            Assert.Single(info.Benchmarks, benchmark => benchmark.Job.Env.Runtime is CoreRuntime);
        }

        [MaxIterationCount(int.MaxValue)]
        public class WithMutator
        {
            [Benchmark] public void Method() { }
        }
        
        [Fact]
        public void JobMutatorsApplySettingsToDefaultJobIfNoneOfTheConfigsContainsJob()
        {
            var info = BenchmarkConverter.TypeToBenchmarks(typeof(WithMutator));
            
            var benchmark = info.Benchmarks.Single();
            
            Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxTargetIterationCount);
        }
        
        [Fact]
        public void OrderOfAppliedAttributesDoesNotAffectMutators()
        {
            var info = BenchmarkConverter.TypeToBenchmarks(typeof(WithMutatorAfterJobAttribute));
            
            var benchmark = info.Benchmarks.Single();
            
            Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxTargetIterationCount);
            Assert.True(benchmark.Job.Env.Runtime is CoreRuntime);
        }

        [MaxIterationCount(int.MaxValue)] // mutator attribute is before job attribute
        [CoreJob]
        public class WithMutatorAfterJobAttribute
        {
            [Benchmark] public void Method() { }
        }
        
        [Fact]
        public void FewMutatorsCanBeAppliedToSameType()
        {
            var info = BenchmarkConverter.TypeToBenchmarks(typeof(WithFewMutators));
            
            var benchmark = info.Benchmarks.Single();
            
            Assert.Equal(1, benchmark.Job.Run.InvocationCount);
            Assert.Equal(1, benchmark.Job.Run.UnrollFactor);
            Assert.Equal(OutlierMode.None, benchmark.Job.Accuracy.OutlierMode);
        }

        [RunOncePerIteration]
        [Outliers(OutlierMode.None)]
        public class WithFewMutators
        {
            [Benchmark] public void Method() { }
        }
    }
}
