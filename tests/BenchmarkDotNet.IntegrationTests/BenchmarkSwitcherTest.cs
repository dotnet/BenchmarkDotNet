using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Configs;

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
            var results = switcher.Run(new[] { "job=Dry", "class=ClassA,ClassC,ClassB", "methods=Method4" });
            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.True(results.All(r => r.BenchmarksCases.All(b => b.Target.Type.Name == "ClassB" && b.Target.Method.Name == "Method4")));
        }

        [Fact]
        public void ConfigPassingTest()
        {
            var types = new[] { typeof(ClassB) };
            var switcher = new BenchmarkSwitcher(types);
            var config = ManualConfig.CreateEmpty();
            MockExporter mockExporter = new MockExporter();
            config.Add(mockExporter);
            switcher.Run(new[] { "job=Dry", "class=ClassB", "methods=Method4" }, config);

            Assert.True(mockExporter.exported);
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

    public class MockExporter : ExporterBase
    {
        public bool exported = false;
        public override void ExportToLog(Summary summary, ILogger logger)
        {
            exported = true;
        }
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
