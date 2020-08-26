using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class GenericBuilderTests
    {
        [Fact]
        public void TestBuildGenericWithOneArgument()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(new[] {typeof(OneArgGenericBenchmark<>)});

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
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(new[] {typeof(TwoArgGenericBenchmark<,>)});

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
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(new[] {typeof(ThreeArgGenericBenchmark<,,>)});

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(ThreeArgGenericBenchmark<int, char, string>));
            Assert.Single(types, typeof(ThreeArgGenericBenchmark<char, string, byte>));
        }

        [GenericTypeArguments(typeof(int), typeof(char),  typeof(string))]
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
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(new[] {typeof(GenericBenchmarkWithWrongAttribute<,>)});

            Assert.Equal(2, types.Length);
            Assert.Single(types, typeof(GenericBenchmarkWithWrongAttribute<int, char>));
            Assert.Single(types, typeof(GenericBenchmarkWithWrongAttribute<char, string>));
        }

        [GenericTypeArguments(typeof(int), typeof(char))]
        [GenericTypeArguments(typeof(char), typeof(string))]
        [GenericTypeArguments(typeof(char))]
        public class GenericBenchmarkWithWrongAttribute<T1, T2>
        {
            [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

            [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
        }

        [Fact]
        public void TestBuildGenericWithConstraints()
        {
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(new[] {typeof(GenericBenchmarkWithConstraints<,>)});

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
            var types = GenericBenchmarksBuilder.GetRunnableBenchmarks(new[] {typeof(GenericBenchmarkWithConstraintsWrongArgs<,>)});

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
    }
}