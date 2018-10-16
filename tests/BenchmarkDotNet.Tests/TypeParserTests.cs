using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Extensions;
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

        [Fact]
        public void CanFilterAllBenchmark()
        {
            var benchmarks = Filter(new [] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, new [] { "--filter", "*" });

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
            var benchmarks = Filter(new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, new[] { "--list", "flat" });

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
            var benchmarks = Filter(new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, new[] { "--list", "flat", "--filter", "*ClassB*" });

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectMethods()
        {
            var benchmarks = Filter(new [] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, new [] { "--filter", "*Method2", "*Method3" });

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
                new[] { "--filter", "BenchmarkDotNet.Tests.ClassA.Method2", "BenchmarkDotNet.Tests.ClassB.Method3" });

            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectClassesUsingPatters()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) },
                new[] { "--filter", "*ClassC*", "*ClassA*" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }
        
        [Fact]
        public void CanSelectClassesUsingTypeNames()
        {
            var benchmarks = Filter(
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) },
                new[] { "--filter", "ClassC", "ClassA" });

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
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC) },
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
                new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD) },
                new[] { "--filter", "*ClassA.Method2", "*ClassA.Method3" });

            Assert.Single(benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
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

        [Fact]
        public void WhenThereIsNothingToFilterAnErrorMessageIsDisplayed()
        {
            var logger = new OutputLogger(Output);
            
            var filtered = Filter(Array.Empty<Type>(), new[] { "--filter", "*" }, logger);
            
            Assert.Empty(filtered);
            Assert.Contains("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.", logger.GetLog());
        }
        
        [Fact]
        public void WhenFilterReturnsNothingAnErrorMessageIsDisplayed()
        {
            var logger = new OutputLogger(Output);
            
            var filtered = Filter(new [] { typeof(ClassA), typeof(ClassB), typeof(ClassC) }, new[] { "--filter", "WRONG" }, logger);
            
            Assert.Empty(filtered);
            Assert.Contains("The filter that you have provided returned 0 benchmarks.", logger.GetLog());
        }
        
        private HashSet<string> Filter(Type[] types, string[] args, ILogger logger = null)
        {
            var config = ConfigParser.Parse(args, logger ?? new OutputLogger(Output));

            return new HashSet<string>(new TypeParser(types, logger ?? new OutputLogger(Output))
                .Filter(config.config, config.options.ListBenchmarkCaseMode != ListBenchmarkCaseMode.Disable)
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