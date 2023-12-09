using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests;
using BenchmarkDotNet.Tests.Loggers;
using JetBrains.Annotations;
using Xunit.Abstractions;
using TypeFilter = BenchmarkDotNet.Running.TypeFilter;

namespace BenchmarkDotNet.Tests
{
    public class TypeFilterTests
    {
        public ITestOutputHelper Output { get; }

        public TypeFilterTests(ITestOutputHelper output) => Output = output;

        [Fact]
        public void ReturnsNoBenchmarksForInvalidTypes()
        {
            var benchmarks = Filter(new[] { typeof(ClassC) }, new[] { "--filter", "*" });

            Assert.Empty(benchmarks);
        }

        [Fact]
        public void CanFilterAllBenchmark()
        {
            var benchmarks = Filter(new[] { typeof(ClassA), typeof(ClassB) }, new[] { "--filter", "*" });

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanFilterAllBenchmarksDuringListAllBenchmarkCase()
        {
            var benchmarks = Filter(new[] { typeof(ClassA), typeof(ClassB) }, new[] { "--list", "flat" });

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanFilterBenchmarksDuringListAllBenchmarkCase()
        {
            var benchmarks = Filter(new[] { typeof(ClassA), typeof(ClassB) }, new[] { "--list", "flat", "--filter", "*ClassB*" });

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectMethods()
        {
            var benchmarks = Filter(new[] { typeof(ClassA), typeof(ClassB) }, new[] { "--filter", "*Method2", "*Method3" });

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectMethodsWithFullName()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB) },
                new[] { "--filter", "BenchmarkDotNet.Tests.ClassA.Method2", "BenchmarkDotNet.Tests.ClassB.Method3" });

            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectClassesUsingPatters()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB) },
                new[] { "--filter", "*ClassC*", "*ClassA*" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanNotSelectClassesUsingTypeNames()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB) },
                new[] { "--filter", "ClassC", "ClassA" });

            Assert.Empty(benchmarks); // it's not supported anymore
        }

        [Fact]
        public void CanSelectClassesWithFullName()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB) },
                new[] { "--filter", "BenchmarkDotNet.Tests.ClassC*", "BenchmarkDotNet.Tests.ClassA*" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectClassesUsingPattern()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB) },
                new[] { "--filter", "BenchmarkDotNet.Tests.Class*A*" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectAttributes()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD) },
                new[] { "--attribute", "Run" });

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassD.Method1", benchmarks);
        }

        [Fact]
        public void CanSelectAttributesWithFullName()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD) },
                new[] { "--attribute", "DontRunAttribute" });

            Assert.Equal(4, benchmarks.Count);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
            Assert.Contains("ClassD.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectNamespaces()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD) },
                new[] { "--filter", "BenchmarkDotNet.Tests*" });

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void ClassAndMethodsCanCombined()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD) },
                new[] { "--filter", "*ClassA.Method2", "*ClassA.Method3" });

            Assert.Single(benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void MethodCanBeFilteredByParameters()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassE), typeof(NOTTests.ClassD) },
                new[] { "--filter", "BenchmarkDotNet.Tests.ClassE.Method1(value: 0)" });

            Assert.Single(benchmarks);
            Assert.Contains("ClassE.Method1", benchmarks);
        }

        [Fact]
        public void GenericTypesCanBeFilteredByDisplayName()
        {
            var benchmarks = Filter(
                new[] { typeof(SomeGeneric<>) },
                new[] { "--filter", "*SomeGeneric<Int32>*" });

            Assert.Single(benchmarks);
            Assert.Contains("SomeGeneric<Int32>.Create", benchmarks);
        }

        private HashSet<string> Filter(Type[] types, string[] args, ILogger? logger = null)
        {
            var nonNullLogger = logger ?? new OutputLogger(Output);

            var config = ConfigParser.Parse(args, nonNullLogger);

            var runnableTypes = TypeFilter.GetTypesWithRunnableBenchmarks(types, Array.Empty<Assembly>(), nonNullLogger);

            return new HashSet<string>(TypeFilter.Filter(config.config, runnableTypes.runnable)
                .SelectMany(runInfo => runInfo.BenchmarksCases)
                .Select(benchmark => $"{benchmark.Descriptor.Type.GetDisplayName()}.{benchmark.Descriptor.WorkloadMethod.Name}"));
        }
    }
}

namespace BenchmarkDotNet.Tests
{
    public class RunAttribute : Attribute { }

    public class DontRunAttribute : Attribute { }

    [Run]
    public class ClassA
    {
        [Benchmark]
        public void Method1() { }

        [Benchmark]
        public void Method2() { }
    }

    [DontRun]
    public class ClassB
    {
        [Benchmark]
        public void Method1() { }

        [Benchmark]
        public void Method2() { }

        [Benchmark]
        public void Method3() { }
    }

    public class ClassC
    {
        // None of these methods are actually Benchmarks!!
        [UsedImplicitly]
        public void Method1() { }

        [UsedImplicitly]
        public void Method2() { }

        [UsedImplicitly]
        public void Method3() { }
    }

    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(string))]
    public class SomeGeneric<T>
    {
        [Benchmark]
        public T Create() => Activator.CreateInstance<T>();
    }

    [Run]
    public class ClassE
    {
        public static IEnumerable<object> Values => new object[]
        {
            uint.MinValue,
            (uint)12345, // same value used by other tests to compare the perf
            uint.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string Method1(uint value) => value.ToString();
    }
}

namespace BenchmarkDotNet.NOTTests
{
    public class ClassD
    {
        [Run]
        [Benchmark]
        public void Method1() { }

        [DontRun]
        [Benchmark]
        public void Method2() { }
    }
}