using System;
using AnotherNamespace;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Tests;
using Lexicographical;
using NamespaceB;
using NamespaceB.NamespaceC;
using Xunit;
using MyClassA = NamespaceB.MyClassA;

namespace BenchmarkDotNet.Tests
{
    public class MisspellingsFinderTests
    {
        [Fact]
        public void CheckNullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => new CorrectionsSuggester(new[] { typeof(NamespaceB.NamespaceC.MyClassC) }).SuggestFor(null));
        }

        [Fact]
        public void FilterUnknown_CollectionIsNotEmpty()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(NamespaceA.MyClassA),
                typeof(NamespaceB.MyClassB),
                typeof(AnotherNamespace.MyClassZ),
                typeof(NamespaceB.NamespaceC.MyClassC)
            }).SuggestFor("Anything");
            Assert.NotEmpty(suggestedNames);
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
            Assert.Equal(2, suggestedNames.Length);
            Assert.Equal(new[] { "NamespaceB.NamespaceC", "NamespaceA.NamespaceC" }, suggestedNames);
        }

        [Fact]
        public void FilterByCompositeNamespace_LexicographicOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(NamespaceA.NamespaceC.MyClassA),
                typeof(NamespaceB.NamespaceC.MyClassC)
            }).SuggestFor("NmespaeB.NamspacC");
            Assert.Equal(new[] { "NamespaceA.NamespaceC.MyClassA.MethodA", "NamespaceB.NamespaceC.MyClassC.MethodC" }, suggestedNames);
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
            Assert.Equal(3, suggestedNames.Length);
            Assert.Equal(new[] { "NamespaceA", "NamespaceB", "NamespaceC" }, suggestedNames);
        }

        [Fact]
        public void FilterByNamespace_LexicographicOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(AnotherNamespace.InnerNamespaceA.MyClassA),
                typeof(NamespaceA.MyClassA)
            }).SuggestFor("Names");
            Assert.Equal(new[]
            {
                "AnotherNamespace.InnerNamespaceA.MyClassA.MethodA",
                "NamespaceA.MyClassA.MethodA",
                "NamespaceA.MyClassA.MethodB"
            }, suggestedNames);
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
            Assert.Equal(2, suggestedNames.Length);
            Assert.Equal(new[] { "InnerNamespaceB", "InnerNamespaceA" }, suggestedNames);
        }

        [Fact]
        public void FilterByInnerNamespace_LexicographicOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(AnotherNamespace.MyClassZ),
                typeof(Lexicographical.MyClassLexicAACDE)
            }).SuggestFor("InerNamespace");
            Assert.Equal(new[] { "AnotherNamespace.MyClassZ.MethodZ", "Lexicographical.MyClassLexicAACDE.MethodA" }, suggestedNames);
        }

        [Fact]
        public void FilterByClassFromDifferentNamespaces()
        {
            var suggestedNames = new CorrectionsSuggester(new[] { typeof(MyClassA), typeof(NamespaceA.MyClassA) })
                .SuggestFor("MyClasA");
            Assert.Equal(1, suggestedNames.Length);
            Assert.Equal("MyClassA", suggestedNames[0]);
        }

        [Fact]
        public void FilterByClass_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(MyClassA), typeof(MyClassB), typeof(MyClassC), typeof(MyClassZ), typeof(NamespaceB.NamespaceC.MyClassA),
                typeof(MyClassZ.MyClassY)
            }).SuggestFor("MyClasZ");
            Assert.Equal(5, suggestedNames.Length);
            Assert.Equal(new[] { "MyClassZ", "MyClassA", "MyClassB", "MyClassC", "MyClassY" }, suggestedNames);
        }

        [Fact]
        public void FilterByClass_LexicographicalOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(MyClassLexicABCDE), typeof(MyClassLexicAACDZ), typeof(MyClassLexicAACDE)
            }).SuggestFor("MyClassLexic");
            Assert.Equal(new[]
            {
                "Lexicographical.MyClassLexicAACDE.MethodA",
                "Lexicographical.MyClassLexicAACDZ.MethodA",
                "Lexicographical.MyClassLexicABCDE.MethodA"
            }, suggestedNames);
        }

        [Fact]
        public void FilterByNamespaceClassMethod_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(MyClassA),
                typeof(NamespaceA.MyClassA),
                typeof(NamespaceB.NamespaceC.MyClassA)
            }).SuggestFor("NamespaceA.MyClasA.MethodA");
            Assert.Equal(new[]
            {
                "NamespaceA.MyClassA.MethodA",
                "NamespaceA.MyClassA.MethodB",
                "NamespaceB.MyClassA.MethodA",
                "NamespaceC.MyClassA.MethodA",
                "NamespaceC.MyClassA.MethodB"
            }, suggestedNames);
        }

        [Fact]
        public void FilterByNamespaceClassMethod_LexicographicalOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
                {
                    typeof(NamespaceB.NamespaceC.MyClassA)
                }).SuggestFor("NamepaceA.yClasA.MetodA");
            Assert.Equal(new[] { "NamespaceB.NamespaceC.MyClassA.MethodA", "NamespaceB.NamespaceC.MyClassA.MethodB" }, suggestedNames);
        }

        [Fact]
        public void FilterGeneric_LevenshteinOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(Generics.GenericA<int>),
                typeof(Generics.GenericB<int>)
            }).SuggestFor("GeneriA<Int32>");
            Assert.Equal(new[] { "GenericA<Int32>", "GenericB<Int32>" }, suggestedNames);
        }

        [Fact]
        public void FilterGeneric_LexicographicalOrdering()
        {
            var suggestedNames = new CorrectionsSuggester(new[]
            {
                typeof(Generics.GenericA<char>),
                typeof(Generics.GenericB<char>)
            }).SuggestFor("GeneriA<Int32>");
            Assert.Equal(new[] { "GenericA<Char>.MethodG1", "GenericB<Char>.MethodG1" }, suggestedNames);
        }
    }
}

namespace Generics
{
    [DontRun]
    public class GenericA<T> where T : new()
    {
        [Benchmark] public T MethodG1() => new T();
    }

    [DontRun]
    public class GenericB<T> where T : new()
    {
        [Benchmark] public T MethodG1() => new T();
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