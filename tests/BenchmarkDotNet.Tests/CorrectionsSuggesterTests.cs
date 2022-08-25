using System;
using AnotherNamespace;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Tests;
using NamespaceB;
using NamespaceB.NamespaceC;
using Xunit;
using MyClassA = NamespaceB.MyClassA;

namespace BenchmarkDotNet.Tests
{
    public class CorrectionsSuggesterTests
    {
        [Fact]
        public void CheckNullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => new CorrectionsSuggester(new[] { typeof(NamespaceB.NamespaceC.MyClassC) }).SuggestFor(null));
        }

        [Fact]
        public void CheckLexicographicalOrder()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(AnotherNamespace.MyClassZ),
                typeof(NamespaceA.MyClassA),
                typeof(NamespaceB.MyClassB),
                typeof(NamespaceB.NamespaceC.MyClassC)
            }).GetAllBenchmarkNames();
            Assert.Equal(new[]
            {
                "AnotherNamespace.MyClassZ.MethodZ",
                "NamespaceA.MyClassA.MethodA",
                "NamespaceA.MyClassA.MethodB",
                "NamespaceB.MyClassB.MethodB",
                "NamespaceB.MyClassB.MethodC",
                "NamespaceB.NamespaceC.MyClassC.MethodC"
            }, suggestedNames);
        }

        [Fact]
        public void FilterUnknownBenchmark_CollectionIsEmpty()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(NamespaceA.MyClassA),
                typeof(NamespaceB.MyClassB),
                typeof(AnotherNamespace.MyClassZ),
                typeof(NamespaceB.NamespaceC.MyClassC)
            }).SuggestFor("Anything");
            Assert.Empty(suggestedNames);
        }

        [Fact]
        public void FilterByCompositeNamespace_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(NamespaceA.NamespaceC.MyClassA),
                typeof(NamespaceB.NamespaceC.MyClassC),
                typeof(AnotherNamespace.InnerNamespaceA.MyClassA)
            }).SuggestFor("NmespaceB.NamespaceC");
            Assert.Equal(new[] { "NamespaceB.NamespaceC*", "NamespaceA.NamespaceC*" }, suggestedNames);
        }

        [Fact]
        public void FilterByNamespace_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(NamespaceA.MyClassA),
                typeof(NamespaceB.MyClassB),
                typeof(AnotherNamespace.MyClassZ),
                typeof(NamespaceB.NamespaceC.MyClassC)
            }).SuggestFor("Nmespace");
            Assert.Equal(new[] { "NamespaceA*", "NamespaceB*" }, suggestedNames);
        }

        [Fact]
        public void FilterByInnerNamespace_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(AnotherNamespace.InnerNamespaceA.MyClassA),
                typeof(AnotherNamespace.InnerNamespaceB.MyClassA),
                typeof(Lexicographical.MyClassLexicAACDE)
            }).SuggestFor("InerNamespaceB");
            Assert.Equal(new[] { "*InnerNamespaceB*" }, suggestedNames);
        }

        [Fact]
        public void FilterByClassFromDifferentNamespaces()
        {
            var suggestedNames = new CorrectionsSuggester(new[] { typeof(MyClassA), typeof(NamespaceA.MyClassA) })
                .SuggestFor("MyClasA");
            Assert.Equal(new[] { "*MyClassA*" }, suggestedNames);
        }

        [Fact]
        public void FilterByClass_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(MyClassA), typeof(MyClassB), typeof(MyClassC), typeof(MyClassZ), typeof(NamespaceB.NamespaceC.MyClassA),
                typeof(MyClassZ.MyClassY)
            }).SuggestFor("MyClasZ");
            Assert.Equal(new[] { "*MyClassZ*" }, suggestedNames);
        }

        [Fact]
        public void FilterByNamespaceClassMethod_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(NamespaceB.MyClassA),
                typeof(NamespaceA.MyClassA),
                typeof(NamespaceB.NamespaceC.MyClassA)
            }).SuggestFor("NamespaceA.MyClasA.MethodA");
            Assert.Equal(new[]
            {
                "NamespaceA.MyClassA.MethodA",
                "NamespaceA.MyClassA.MethodB",
                "NamespaceB.MyClassA.MethodA",
            }, suggestedNames);
        }

        [Fact]
        public void FilterGeneric_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(Generics.GenericA<int>),
                typeof(Generics.GenericB<int>)
            }).SuggestFor("GeneriA<Int32>");
            Assert.Equal(new[] { "*GenericA<Int32>*" }, suggestedNames);
        }
    }
}

namespace Generics
{
    [DontRun]
    public class GenericA<T>
    {
        [Benchmark] public void MethodG1() { }
    }

    [DontRun]
    public class GenericB<T>
    {
        [Benchmark] public void MethodG1() { }
    }
}

namespace NamespaceA
{
    [DontRun] public class MyClassA
    {
        [Benchmark] public void MethodA() { }

        [Benchmark] public void MethodB() { }
    }

    namespace NamespaceC
    {
        [DontRun] public class MyClassA
        {
            [Benchmark] public void MethodA() { }
        }
    }
}

namespace NamespaceB
{
    [DontRun] public class MyClassA
    {
        [Benchmark] public void MethodA() { }
    }

    [DontRun] public class MyClassB
    {
        [Benchmark] public void MethodB() { }

        [Benchmark] public void MethodC() { }
    }

    namespace NamespaceC
    {
        [DontRun] public class MyClassA
        {
            [Benchmark] public void MethodA() { }

            [Benchmark] public void MethodB() { }
        }

        [DontRun] public class MyClassC
        {
            [Benchmark] public void MethodC() { }
        }
    }
}

namespace AnotherNamespace
{
    [DontRun] public class MyClassZ
    {
        [Benchmark] public void MethodZ() { }

        [DontRun] public class MyClassY
        {
            [Benchmark] public void MethodY() { }
        }
    }

    namespace InnerNamespaceA
    {
        [DontRun] public class MyClassA
        {
            [Benchmark] public void MethodA() { }
        }
    }

    namespace InnerNamespaceB
    {
        [DontRun] public class MyClassA
        {
            [Benchmark] public void MethodA() { }
        }
    }
}

namespace Lexicographical
{
    [DontRun] public class MyClassLexicABCDE
    {
        [Benchmark] public void MethodA() { }
    }

    [DontRun] public class MyClassLexicAACDE
    {
        [Benchmark] public void MethodA() { }
    }

    [DontRun] public class MyClassLexicAACDZ
    {
        [Benchmark] public void MethodA() { }
    }
}