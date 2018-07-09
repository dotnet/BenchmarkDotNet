using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests;
using BenchmarkDotNet.Tests.Loggers;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class TypeParserTests
    {
        public ITestOutputHelper Output { get; }

        public TypeParserTests(ITestOutputHelper output) => Output = output;
        
        private HashSet<string> Filter(Type[] types, string[] args)
            => new HashSet<string>(new BenchmarkSwitcher(types).Filter(ConfigParser.Parse(args, new OutputLogger(Output)).config)
                .SelectMany(runInfo => runInfo.BenchmarksCases)
                .Select(benchmark => $"{benchmark.Descriptor.Type.Name}.{benchmark.Descriptor.WorkloadMethod.Name}"));

        [Fact]
        public void CanSelectMethods()
        {
            var benchmarks = Filter(new [] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, new [] { "--method", "Method2", "Method3" });

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectMethodsWithFullName()
        {
            var benchmarks = Filter(
                new [] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, 
                new[] { "--method", "BenchmarkDotNet.Tests.ClassA.Method2", "BenchmarkDotNet.Tests.ClassB.Method3" });

            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectClasses()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) },
                new[] { "--class", "ClassC", "ClassA" });

            // TODO do we want to allow "class = ClassC, ClassA" aswell as "class=ClassC,ClassA"
            //var matches = typeParser.MatchingTypesWithMethods(new[] { "class = ClassC, ClassA" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectClassesWithFullName()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) },
                new[] { "--class", "BenchmarkDotNet.Tests.ClassC", "BenchmarkDotNet.Tests.ClassA" });
            // TODO do we want to allow "class = ClassC, ClassA" aswell as "class=ClassC,ClassA"
            //var matches = typeParser.MatchingTypesWithMethods(new[] { "class = ClassC, ClassA" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectAttributes()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD) },
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
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD) },
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
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD) },
                new[] { "--namespace", "BenchmarkDotNet.Tests" });

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void ClassAndMethodShouldCombineAsAndFilters() // #249
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD) },
                new[] { "--method", "Method2", "Method3", "--class", "ClassA" });

            Assert.Single(benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
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