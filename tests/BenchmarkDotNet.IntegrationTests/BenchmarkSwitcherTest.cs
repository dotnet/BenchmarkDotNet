using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Linq;
using BenchmarkDotNet.Attributes.Jobs;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkSwitcherTest
    {
        [Fact]
        public void CmdLineParsingTest()
        {
            // Don't cover every combination, just pick a complex scenarion and check
            // it works end-to-end, i.e. "method=Method1" and "class=ClassB"
            var types = new[] { typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(NOTIntegrationTests.ClassD) };
            var switcher = new BenchmarkSwitcher(types);

            // BenchmarkSwitcher only picks up config values via the args passed in, not via class annotations (e.g "[DryConfig]")
            var results = switcher.Run(new[] { "job=Dry", "class=ClassA,ClassC", "methods=Method4" });
            Assert.Equal(2, results.Count());
            Assert.Equal(3, results.SelectMany(r => r.Benchmarks).Count());
            Assert.True(results.Any(r => r.Benchmarks.Any(b => b.Target.Type.Name == "ClassA" && b.Target.Method.Name == "Method1")));
            Assert.True(results.Any(r => r.Benchmarks.Any(b => b.Target.Type.Name == "ClassA" && b.Target.Method.Name == "Method2")));
            Assert.True(results.Any(r => r.Benchmarks.Any(b => b.Target.Type.Name == "ClassB" && b.Target.Method.Name == "Method4")));
        }
    }
}

namespace BenchmarkDotNet.IntegrationTests
{
    public class ClassA
    {
        [Benchmark]
        public void Method1() { }
        [Benchmark]
        public void Method2() { }
    }

    public class ClassB
    {
        [Benchmark]
        public void Method1() { }
        [Benchmark]
        public void Method2() { }
        [Benchmark]
        public void Method3() { }
        [Benchmark]
        public void Method4() { }
    }

    public class ClassC
    {
        // None of these methods are actually Benchmarks!!
        public void Method1() { }
        public void Method2() { }
        public void Method3() { }
    }
}

namespace BenchmarkDotNet.NOTIntegrationTests
{
    [DryJob]
    public class ClassD
    {
        [Benchmark]
        public void Method1() { }
        [Benchmark]
        public void Method2() { }
    }
}
