using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class TypeParserTests
    {
        private readonly ITestOutputHelper output;

        public TypeParserTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private class Matcher
        {
            private readonly ITestOutputHelper output;
            private readonly TypeParser typeParser;

            public Matcher(ITestOutputHelper output, params Type[] types)
            {
                this.output = output;
                typeParser = new TypeParser(types, ConsoleLogger.Default);

                output.WriteLine("Classes:");
                foreach (var type in types)
                    output.WriteLine("  " + type.FullName);
            }

            public List<TypeParser.TypeWithMethods> Match(params string[] args)
            {
                var items = typeParser.MatchingTypesWithMethods(args).ToList();

                output.WriteLine("MatchResult for [" + string.Join(", ", args.Select(arg => $"\"{arg}\"")) + "]:");
                foreach (var item in items)
                    output.WriteLine("  " + item.Type.Name);

                return items;
            }
        }

        [Fact]
        public void CanSelectMethods()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var matches = matcher.Match("method=Method2,Method3");

            Assert.Equal(2, matches.Count);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA" &&
                                                   match.Methods.Any(m => m.Name == "Method2")));
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassB" &&
                                                   match.Methods.Any(m => m.Name == "Method2") &&
                                                   match.Methods.Any(m => m.Name == "Method3")));
        }

        [Fact]
        public void CanSelectMethodsWithFullName()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var matches = matcher.Match("method=BenchmarkDotNet.Tests.ClassA.Method2,BenchmarkDotNet.Tests.ClassB.Method3");

            Assert.Equal(2, matches.Count);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA" &&
                                                   match.Methods.All(m => m.Name == "Method2")));
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassB" &&
                                                   match.Methods.All(m => m.Name == "Method3")));
        }

        [Fact]
        public void CanSelectClasses()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var matches = matcher.Match("class=ClassC,ClassA");
            // TODO do we want to allow "class = ClassC, ClassA" aswell as "class=ClassC,ClassA"
            //var matches = typeParser.MatchingTypesWithMethods(new[] { "class = ClassC, ClassA" });

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Single(matches);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA" && match.AllMethodsInType));
        }

        [Fact]
        public void CanSelectClassesWithFullName()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var matches = matcher.Match("class=BenchmarkDotNet.Tests.ClassC,BenchmarkDotNet.Tests.ClassA");

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Single(matches);
            Assert.Single(matches.Where(match => match.Type.Name == "ClassA" && match.AllMethodsInType));
        }

        [Fact]
        public void CanSelectAttributes()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD));
            var matches = matcher.Match("attribute=Run");

            // Find entire classes or individual methods that have the [Run] attribute
            Assert.Equal(2, matches.Count);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA" && match.AllMethodsInType));
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassD" && match.Methods.All(m => m.Name == "Method1")));
        }

        [Fact]
        public void CanSelectAttributesWithFullName()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD));
            var matches = matcher.Match("attribute=DontRunAttribute");

            // Find entire classes or individual methods that have the [DontRun] attribute
            Assert.Equal(2, matches.Count);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassB" && match.AllMethodsInType));
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassD" && match.Methods.All(m => m.Name == "Method2")));
        }

        [Fact]
        public void CanSelectNamespaces()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD));
            var matches = matcher.Match("namespace=BenchmarkDotNet.Tests");

            Assert.Equal(2, matches.Count);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA" && match.AllMethodsInType));
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassB" && match.AllMethodsInType));
        }

        [Fact]
        public void CanSelectPluralVersions()
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTTests.ClassD));

            // Note we are using "classes" here rather than "class" (we want to be nicer to our users!!)
            // Likewise you can also use "methods" and "namespaces"
            var matches = matcher.Match("classes=ClassC,ClassA", "methods=Method2");

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            // ClassA only Method2 got matched because it it's classes AND methods #249
            Assert.Single(matches);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA" && match.Methods.All(m => m.Name == "Method2")));
        }

        [Fact]
        public void ClassAndMethodShouldCombineAsAndFilters() // #249
        {
            var matcher = new Matcher(output, typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var matches = matcher.Match("method=Method2,Method3", "class=ClassA");

            Assert.Single(matches);
            Assert.Equal(1, matches.Count(match => match.Type.Name == "ClassA"
                                                   && match.Methods.All(m => m.Name == "Method2")));
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