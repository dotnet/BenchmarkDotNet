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
            BenchmarkCase benchmarkCase = BenchmarkConverter.TypeToBenchmarks(derivedType).BenchmarksCases.Single();

            Assert.NotNull(benchmarkCase);
            Assert.NotNull(benchmarkCase.Descriptor);

            Assert.NotNull(benchmarkCase.Descriptor.IterationSetupMethod);
            Assert.Equal(benchmarkCase.Descriptor.IterationSetupMethod.DeclaringType, derivedType);

            Assert.NotNull(benchmarkCase.Descriptor.IterationCleanupMethod);
            Assert.Equal(benchmarkCase.Descriptor.IterationCleanupMethod.DeclaringType, derivedType);

            Assert.NotNull(benchmarkCase.Descriptor.GlobalCleanupMethod);
            Assert.Equal(benchmarkCase.Descriptor.GlobalCleanupMethod.DeclaringType, derivedType);

            Assert.NotNull(benchmarkCase.Descriptor.GlobalSetupMethod);
            Assert.Equal(benchmarkCase.Descriptor.GlobalSetupMethod.DeclaringType, derivedType);
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
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived)).BenchmarksCases.Single();
            
            Assert.Equal(1, benchmark.Job.Run.InvocationCount);
            Assert.Equal(1, benchmark.Job.Run.UnrollFactor);
        }

        [Fact]
        public void IfIterationCleanupIsProvidedTheBenchmarkShouldRunOncePerIteration()
        {
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithIterationCleanupOnly)).BenchmarksCases.Single();
            
            Assert.Equal(1, benchmark.Job.Run.InvocationCount);
            Assert.Equal(1, benchmark.Job.Run.UnrollFactor);
        }

        public class WithIterationCleanupOnly
        {
            [IterationCleanup] public void Cleanup() { }
            [Benchmark] public void Benchmark() { }
        }
        
        [Fact]
        public void InvocationCountIsRespectedForBenchmarksWithIterationSetup()
        {
            const int InvocationCount = 100;
            
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived), 
                DefaultConfig.Instance.With(Job.Default
                    .WithInvocationCount(InvocationCount)))
                .BenchmarksCases.Single();
            
            Assert.Equal(InvocationCount, benchmark.Job.Run.InvocationCount);
            Assert.NotNull(benchmark.Descriptor.IterationSetupMethod);
        }
        
        [Fact]
        public void UnrollFactorIsRespectedForBenchmarksWithIterationSetup()
        {
            const int UnrollFactor = 13;
            
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(Derived), 
                    DefaultConfig.Instance.With(Job.Default
                        .WithUnrollFactor(UnrollFactor)))
                .BenchmarksCases.Single();
            
            Assert.Equal(UnrollFactor, benchmark.Job.Run.UnrollFactor);
            Assert.NotNull(benchmark.Descriptor.IterationSetupMethod);
        }

        [Fact]
        public void JobMutatorsApplySettingsToAllNonMutatorJobs()
        {
            var info = BenchmarkConverter.TypeToBenchmarks(
                    typeof(WithMutator), 
                    DefaultConfig.Instance
                        .With(Job.Clr)
                        .With(Job.Core));
            
            Assert.Equal(2, info.BenchmarksCases.Length);
            Assert.All(info.BenchmarksCases, benchmark => Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxIterationCount));
            Assert.Single(info.BenchmarksCases, benchmark => benchmark.Job.Environment.Runtime is ClrRuntime);
            Assert.Single(info.BenchmarksCases, benchmark => benchmark.Job.Environment.Runtime is CoreRuntime);
            Assert.All(info.BenchmarksCases, benchmark => Assert.False(benchmark.Job.Meta.IsMutator)); // the job does not became a mutator itself, this config should not be copied
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
            
            var benchmark = info.BenchmarksCases.Single();
            
            Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxIterationCount);
            Assert.False(benchmark.Job.Meta.IsMutator);
        }
        
        [Fact]
        public void OrderOfAppliedAttributesDoesNotAffectMutators()
        {
            var info = BenchmarkConverter.TypeToBenchmarks(typeof(WithMutatorAfterJobAttribute));
            
            var benchmark = info.BenchmarksCases.Single();
            
            Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxIterationCount);
            Assert.True(benchmark.Job.Environment.Runtime is CoreRuntime);
            Assert.False(benchmark.Job.Meta.IsMutator);
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
            
            var benchmarkCase = info.BenchmarksCases.Single();
            
            Assert.Equal(1, benchmarkCase.Job.Run.InvocationCount);
            Assert.Equal(1, benchmarkCase.Job.Run.UnrollFactor);
            Assert.Equal(OutlierMode.DontRemove, benchmarkCase.Job.Accuracy.OutlierMode);
            Assert.False(benchmarkCase.Job.Meta.IsMutator);
        }

        [RunOncePerIteration]
        [Outliers(OutlierMode.DontRemove)]
        public class WithFewMutators
        {
            [Benchmark] public void Method() { }
        }
    }
}
