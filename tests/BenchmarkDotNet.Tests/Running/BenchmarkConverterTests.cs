using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Running;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Tests.Running
{
    public partial class BenchmarkConverterTests
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
                DefaultConfig.Instance.AddJob(Job.Default
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
                    DefaultConfig.Instance.AddJob(Job.Default
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
                        .AddJob(Job.Default.WithRuntime(ClrRuntime.Net472))
                        .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)));

            Assert.Equal(2, info.BenchmarksCases.Length);
            Assert.All(info.BenchmarksCases, benchmark => Assert.Equal(int.MaxValue, benchmark.Job.Run.MaxIterationCount));
            Assert.Single(info.BenchmarksCases, benchmark => benchmark.GetRuntime() is ClrRuntime);
            Assert.Single(info.BenchmarksCases, benchmark => benchmark.GetRuntime() is CoreRuntime);
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
            Assert.True(benchmark.GetRuntime() is CoreRuntime);
            Assert.False(benchmark.Job.Meta.IsMutator);
        }

        [MaxIterationCount(int.MaxValue)] // mutator attribute is before job attribute
        [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50)]
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

        [Fact]
        public void MethodDeclarationOrderIsPreserved()
        {
            foreach (Type type in new[] { typeof(BAC), typeof(BAC_Partial), typeof(BAC_Partial_DifferentFiles) })
            {
                var info = BenchmarkConverter.TypeToBenchmarks(type);

                Assert.Equal(nameof(BAC.B), info.BenchmarksCases[0].Descriptor.WorkloadMethod.Name);
                Assert.Equal(nameof(BAC.A), info.BenchmarksCases[1].Descriptor.WorkloadMethod.Name);
                Assert.Equal(nameof(BAC.C), info.BenchmarksCases[2].Descriptor.WorkloadMethod.Name);
            }
        }

        public class BAC
        {
            // BAC is not sorted in either descending or ascending way
            [Benchmark] public void B() { }
            [Benchmark] public void A() { }
            [Benchmark] public void C() { }
        }

        public partial class BAC_Partial
        {
            [Benchmark] public void B() { }
            [Benchmark] public void A() { }
        }

        public partial class BAC_Partial
        {
            [Benchmark] public void C() { }
        }

        public partial class BAC_Partial_DifferentFiles
        {
            [Benchmark] public void A() { }
            [Benchmark] public void C() { }
        }

        [Theory]
        [InlineData(typeof(PrivateGlobalSetup))]
        [InlineData(typeof(PrivateGlobalCleanup))]
        [InlineData(typeof(PrivateIterationSetup))]
        [InlineData(typeof(PrivateIterationCleanup))]
        public void ReportsSetupAndCleanupMethodsThatAreNonPublic(Type type)
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(type);

            Assert.Contains("method X has incorrect access modifiers", Assert.Single(runInfo.DeclarationErrors).Message);
            Assert.NotEmpty(runInfo.BenchmarksCases);
        }

        public class PrivateGlobalSetup
        {
            [GlobalSetup] private void X() { }
            [Benchmark] public void A() { }
        }

        public class PrivateGlobalCleanup
        {
            [GlobalCleanup] private void X() { }
            [Benchmark] public void A() { }
        }

        public class PrivateIterationSetup
        {
            [IterationSetup] private void X() { }
            [Benchmark] public void A() { }
        }

        public class PrivateIterationCleanup
        {
            [IterationCleanup] private void X() { }
            [Benchmark] public void A() { }
        }

        [Fact]
        public void EveryBadDeclarationInATypeIsReported()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(SeveralBadDeclarations));

            Assert.Equal(
                [
                    "Benchmark method Generic is generic.\nGeneric Benchmark methods are not supported.",
                    "Benchmark method NonPublic has incorrect access modifiers.\nMethod must be public.",
                    "Benchmark method TakesAnArgument has incorrect signature.\nMethod shouldn't have any arguments.",
                    "GlobalSetup method Setup has incorrect access modifiers.\nMethod must be public."
                ],
                runInfo.DeclarationErrors.Select(error => error.Message).OrderBy(message => message));

            Assert.All(runInfo.DeclarationErrors, error => Assert.True(error.IsCritical));
            Assert.Equal(nameof(SeveralBadDeclarations.Fine), Assert.Single(runInfo.BenchmarksCases).Descriptor.WorkloadMethod.Name);
        }

#pragma warning disable BDN1103, BDN1104, BDN1400
        public class SeveralBadDeclarations
        {
            [GlobalSetup] private void Setup() { }

            [Benchmark] private void NonPublic() { }
            [Benchmark] public void Generic<T>() { }
            [Benchmark] public void TakesAnArgument(int x) { }
            [Benchmark] public void Fine() { }
        }
#pragma warning restore BDN1103, BDN1104, BDN1400

        [Fact]
        public void AParameterMemberThatCannotBeReadLeavesTheOthersRanging()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(OneUnreadableParameterMember));

            Assert.Contains("has no public, accessible method/property called Missing", Assert.Single(runInfo.DeclarationErrors).Message);
            Assert.Equal([1, 2], runInfo.BenchmarksCases.Select(benchmark => benchmark.Parameters["Good"]));
        }

        public class OneUnreadableParameterMember
        {
            [Params(1, 2)] public int Good { get; set; }
            [ParamsSource("Missing")] public int Bad { get; set; }

            [Benchmark] public void Run() { }
        }

        [Fact]
        public void AnOpenGenericTypeIsReportedWithoutCases()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(OpenGeneric<>));

            Assert.Contains("use BenchmarkSwitcher for it", Assert.Single(runInfo.DeclarationErrors).Message);
            Assert.Empty(runInfo.BenchmarksCases);
            Assert.False(runInfo.ContainsBenchmarkDeclarations);
        }

        public class OpenGeneric<T>
        {
            [Benchmark] public void Run() { }
        }

        [Fact]
        public void EveryRefusedArgumentOfAMethodIsReported()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(SeveralRefusedArguments));

            Assert.Equal(
                [
                    "[Arguments] on Run provides null for the Int32 parameter 'a', which is a value type - null is not one of its values.",
                    "[Arguments] on Run provides null for the Int64 parameter 'b', which is a value type - null is not one of its values.",
                    "Benchmark Run has invalid number of defined arguments provided with [Arguments]! 3 instead of 2."
                ],
                runInfo.DeclarationErrors.Select(error => error.Message).OrderBy(message => message));
        }

#pragma warning disable BDN1501, BDN1502
        public class SeveralRefusedArguments
        {
            [Benchmark]
            [Arguments(null, null)]
            [Arguments(1, 2, 3)]
            public void Run(int a, long b) { }
        }
#pragma warning restore BDN1501, BDN1502

        [Fact]
        public void TheSameRefusalIsReportedOnce()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(TwoRefusedParamsValues));

            Assert.Contains("[Params] provides null for the Int32 member 'Value'", Assert.Single(runInfo.DeclarationErrors).Message);
            Assert.Equal([1], runInfo.BenchmarksCases.Select(benchmark => benchmark.Parameters["Value"]));
        }

#pragma warning disable BDN1301
        public class TwoRefusedParamsValues
        {
            [Params(null, 1, null)] public int Value { get; set; }

            [Benchmark] public void Run() { }
        }
#pragma warning restore BDN1301

        [Fact]
        public void ACaseFromAnotherTypeIsRefused()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(WithMutator));
            var foreign = BenchmarkConverter.TypeToBenchmarks(typeof(Derived)).BenchmarksCases;

            var exception = Assert.Throws<ArgumentException>(() => runInfo.WithBenchmarks(foreign));

            Assert.Contains("Derived.Test is not declared by WithMutator", exception.Message);
        }

        [Fact]
        public void AMethodFromAnotherTypeIsRefused()
        {
            var own = typeof(WithMutator).GetMethod(nameof(WithMutator.Method))!;
            var foreign = typeof(WithIterationCleanupOnly).GetMethod(nameof(WithIterationCleanupOnly.Cleanup))!;

            Assert.Contains("WithIterationCleanupOnly.Cleanup is not declared by WithMutator",
                Assert.Throws<ArgumentException>(() => new Descriptor(typeof(WithMutator), foreign)).Message);

            Assert.Contains("WithIterationCleanupOnly.Cleanup is not declared by WithMutator",
                Assert.Throws<ArgumentException>(() => new Descriptor(typeof(WithMutator), own, iterationCleanupMethod: foreign)).Message);
        }

        [Fact]
        public void AnInheritedMethodIsAccepted()
        {
            var inherited = typeof(Base).GetMethod(nameof(Base.Test))!;

            Assert.Equal(typeof(Derived), new Descriptor(typeof(Derived), inherited).Type);
        }

        [Fact]
        public void NarrowingAReadingKeepsWhatItLearned()
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(OneUnreadableParameterMember));

            var narrowed = runInfo.WithBenchmarks([.. runInfo.BenchmarksCases.Take(1)]);

            Assert.Equal(runInfo.DeclarationErrors, narrowed.DeclarationErrors);
            Assert.Equal(runInfo.Type, narrowed.Type);
            Assert.Single(narrowed.BenchmarksCases);
        }
    }
}
