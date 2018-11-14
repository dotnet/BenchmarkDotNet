using System;
using BenchmarkDotNet.All;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.ConsoleArguments;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class MisspellingsFinderTests
    {
        [Fact]
        public void CheckNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) }).Find(null));
        }

        [Fact]
        public void CheckEmpty()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) }).Find("");
            Assert.Empty(foundNames);
        }

        [Fact]
        public void CheckMisspelling_NamespaceClassMethod()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) })
                .Find("BenchmarkDotNet.All.ClassNameNumbrOne.MethodClassOneNameNumberOne");
            Assert.Single(foundNames);
            Assert.Contains("BenchmarkDotNet.All.ClassNameNumberOne.MethodClassOneNameNumberOne", foundNames);
        }

        [Fact]
        public void CheckMisspelling_NamespaceClass()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) })
                .Find("BenchmarkDotNet.All.ClassNameNumbrOne");
            Assert.Single(foundNames);
            Assert.Contains("BenchmarkDotNet.All.ClassNameNumberOne", foundNames);
        }

        [Fact]
        public void CheckMisspelling_Namespace()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) })
                .Find("BechmarkDotNet.All");
            Assert.Single(foundNames);
            Assert.Contains("BenchmarkDotNet.All", foundNames);
        }

        [Fact]
        public void CheckMultipleResults_MisspellingNamespaceClassMethod()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) })
                .Find("BenchmarkDotNet.All.ClassNameNumberOne.MethodClassOneNameNumber");
            Assert.Contains("BenchmarkDotNet.All.ClassNameNumberOne.MethodClassOneNameNumberOne", foundNames);
            Assert.Contains("BenchmarkDotNet.All.ClassNameNumberOne.MethodClassOneNameNumberTwo", foundNames);
        }

        [Fact]
        public void CheckMultipleMisspelling_NamespaceClass()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne), typeof(ClassNameNumberTwo) })
                .Find("BenchmarkDotNet.All.ClassNameNumber");
            Assert.Contains("BenchmarkDotNet.All.ClassNameNumberOne", foundNames);
            Assert.Contains("BenchmarkDotNet.All.ClassNameNumberTwo", foundNames);
        }

        [Fact]
        public void CheckMultipleMisspelling_Namespace()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(BenchmarkDotNet.One.ClassNameNumberOne), typeof(ClassNameNumberOne) })
                .Find("BenchmarkDotNet.Al");
            Assert.Contains("BenchmarkDotNet.All", foundNames);
            Assert.Contains("BenchmarkDotNet.One", foundNames);
        }

        [Fact]
        public void CheckNotFound()
        {
            var foundNames = new MisspellingsFinder(new[] { typeof(ClassNameNumberOne) })
                .Find("BenchmarkDotNet.All.ClasNameNumerOne.MetodClassOneNameNmberThree");
            Assert.Empty(foundNames);
        }
    }
}

namespace BenchmarkDotNet.All
{
    public class DontRunAttribute : Attribute { }

    [DontRun]
    public class ClassNameNumberOne
    {
        [Benchmark]
        public void MethodClassOneNameNumberOne() { }

        [Benchmark]
        public void MethodClassOneNameNumberTwo() { }

        [Benchmark]
        public void MethodClassOneNameNumberThree() { }
    }

    [DontRun]
    public class ClassNameNumberTwo
    {
        [Benchmark]
        public void MethodClassTwoNameNumberOne() { }

        [Benchmark]
        public void MethodClassTwoNameNumberTwo() { }

        [Benchmark]
        public void MethodClassTwoNameNumberThree() { }
    }
}

namespace BenchmarkDotNet.One
{
    [DontRun]
    public class ClassNameNumberOne
    {
        [Benchmark]
        public void MethodClassOneNameNumberOne() { }

        [Benchmark]
        public void MethodClassOneNameNumberTwo() { }

        [Benchmark]
        public void MethodClassOneNameNumberThree() { }
    }

    [DontRun]
    public class ClassNameNumberTwo
    {
        [Benchmark]
        public void MethodClassTwoNameNumberOne() { }

        [Benchmark]
        public void MethodClassTwoNameNumberTwo() { }

        [Benchmark]
        public void MethodClassTwoNameNumberThree() { }
    }
}