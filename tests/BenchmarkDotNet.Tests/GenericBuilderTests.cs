using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Tests
{
    public class GenericBuilderTests
    {
        [Fact]
        public void TestBuildGenericWithOneArgument()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks([typeof(OneArgGenericBenchmark<>)]);

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(OneArgGenericBenchmark<int>));
            Assert.Single(types, typeof(OneArgGenericBenchmark<char>));
        }

        [GenericTypeArguments(typeof(int))]
        [GenericTypeArguments(typeof(char))]
        public class OneArgGenericBenchmark<T>
        {
            [Benchmark] public T CreateT() => Activator.CreateInstance<T>();
        }

        [Fact]
        public void TestBuildGenericWithTwoArguments()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks([typeof(TwoArgGenericBenchmark<,>)]);

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(TwoArgGenericBenchmark<int, char>));
            Assert.Single(types, typeof(TwoArgGenericBenchmark<char, string>));
        }

        [GenericTypeArguments(typeof(int), typeof(char))]
        [GenericTypeArguments(typeof(char), typeof(string))]
        public class TwoArgGenericBenchmark<T1, T2>
        {
            [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

            [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
        }

        [Fact]
        public void TestBuildGenericWithThreeArguments()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks([typeof(ThreeArgGenericBenchmark<,,>)]);

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(ThreeArgGenericBenchmark<int, char, string>));
            Assert.Single(types, typeof(ThreeArgGenericBenchmark<char, string, byte>));
        }

        [GenericTypeArguments(typeof(int), typeof(char), typeof(string))]
        [GenericTypeArguments(typeof(char), typeof(string), typeof(byte))]
        public class ThreeArgGenericBenchmark<T1, T2, T3>
        {
            [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

            [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();

            [Benchmark] public T3 CreateT3() => Activator.CreateInstance<T3>();
        }

        [Fact]
        public void TestBuildGenericWithWrongAttributes()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks([typeof(GenericBenchmarkWithWrongAttribute<,>)]);

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(GenericBenchmarkWithWrongAttribute<int, char>));
            Assert.Single(types, typeof(GenericBenchmarkWithWrongAttribute<char, string>));
        }

        [GenericTypeArguments(typeof(int), typeof(char))]
        [GenericTypeArguments(typeof(char), typeof(string))]
#pragma warning disable BDN1102
        [GenericTypeArguments(typeof(char))]
#pragma warning restore BDN1102
        public class GenericBenchmarkWithWrongAttribute<T1, T2>
        {
            [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

            [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
        }

        [Fact]
        public void TestBuildGenericWithConstraints()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks([typeof(GenericBenchmarkWithConstraints<,>)]);

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(GenericBenchmarkWithConstraints<int, char>));
            Assert.Single(types, typeof(GenericBenchmarkWithConstraints<char, byte>));
        }

        [GenericTypeArguments(typeof(int), typeof(char))]
        [GenericTypeArguments(typeof(char), typeof(byte))]
        public class GenericBenchmarkWithConstraints<T1, T2> where T1 : struct
            where T2 : struct
        {
            [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

            [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
        }

        [Fact]
        public void TestBuildGenericWithConstraintsWrongArgs()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks([typeof(GenericBenchmarkWithConstraintsWrongArgs<,>)]);

            Assert.Single(types);
            Assert.Single(types, typeof(GenericBenchmarkWithConstraintsWrongArgs<int, char>));
        }

        [GenericTypeArguments(typeof(int), typeof(char))]
        [GenericTypeArguments(typeof(char), typeof(string))]
        public class GenericBenchmarkWithConstraintsWrongArgs<T1, T2> where T1 : struct
            where T2 : struct
        {
            [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

            [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
        }

        [Fact]
        public void TestTypeWithUnreadableAttributesIsDropped()
        {
            // Reflection constructs every attribute of a type in order to hand any of them back, so a [Config] that
            // cannot be instantiated makes the read of the [GenericTypeArguments] throw. The type is unusable at that
            // point - BenchmarkConverter would throw on the very same read - so it is dropped, and the benchmarks
            // that were listed next to it are still returned.
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(
                [typeof(BenchmarkWithAbstractConfig), typeof(BenchmarkWithInaccessibleConfig), typeof(OneArgGenericBenchmark<>)]);

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(OneArgGenericBenchmark<int>));
            Assert.Single(types, typeof(OneArgGenericBenchmark<char>));
        }

        [Fact]
        public void TestTypeWithUnreadableAttributesIsReportedAsAFailure()
        {
            var built = GenericBenchmarksBuilder.BuildGenericsIfNeeded(typeof(BenchmarkWithAbstractConfig)).ToArray();

            var failure = Assert.Single(built);
            Assert.False(failure.IsSuccess);
            Assert.Contains(nameof(BenchmarkWithAbstractConfig), failure.Error);

            // Told apart from a [GenericTypeArguments] that did not fit, because only this kind has to be reported
            // by whoever drops it: GenericBenchmarksValidator needs a surviving benchmark before it ever runs.
            Assert.True(failure.IsUnreadable);
        }

        [Fact]
        public void TestGenericTypeThatFailedToBuildIsNotReportedAsUnreadable()
        {
            var built = GenericBenchmarksBuilder.BuildGenericsIfNeeded(typeof(GenericBenchmarkWithConstraintsWrongArgs<,>)).ToArray();

            var failure = Assert.Single(built, candidate => !candidate.IsSuccess);
            Assert.False(failure.IsUnreadable);
            Assert.Contains("wrong type argument", failure.Error);
        }

        [Config(typeof(DebugConfig))] // abstract, so ConfigAttribute's constructor throws
        public class BenchmarkWithAbstractConfig
        {
            [Benchmark] public int Identity() => 1;
        }

        [Config(typeof(NoPublicConstructorConfig))]
        public class BenchmarkWithInaccessibleConfig
        {
            [Benchmark] public int Identity() => 1;

            private class NoPublicConstructorConfig : ManualConfig
            {
                private NoPublicConstructorConfig() { }
            }
        }
    }
}