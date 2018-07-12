using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

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
            var results = switcher.Run(new[] { "-j", "Dry", "--filter", "*ClassB.Method4" });
            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Dry)));
            Assert.True(results.All(r => r.BenchmarksCases.All(b => b.Descriptor.Type.Name == "ClassB" && b.Descriptor.WorkloadMethod.Name == "Method4")));
        }

        [Fact]
        public void WhenJobIsDefinedInTheConfigAndArgumentsDontContainJobArgumentOnlySingleJobIsUsed()
        {
            var types = new[] { typeof(ClassB) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithJobDefined = ManualConfig.CreateEmpty().With(mockExporter).With(Job.Dry);
            
            var results = switcher.Run(new[] { "--filter", "*Method3" }, configWithJobDefined);

            Assert.True(mockExporter.exported);
            
            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Dry)));
        }
        
        [Fact]
        public void WhenJobIsDefinedViaAttributeAndArgumentsDontContainJobArgumentOnlySingleJobIsUsed()
        {
            var types = new[] { typeof(WithDryAttribute) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithoutJobDefined = ManualConfig.CreateEmpty().With(mockExporter);
            
            var results = switcher.Run(new[] { "--filter", "*WithDryAttribute*" }, configWithoutJobDefined);

            Assert.True(mockExporter.exported);
            
            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Dry)));
        }
        
        [Fact]
        public void JobNotDefinedButStillBenchmarkIsExecuted()
        {
            var types = new[] { typeof(JustBenchmark) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithoutJobDefined = ManualConfig.CreateEmpty().With(mockExporter);
            
            var results = switcher.Run(new[] { "--filter", "*" }, configWithoutJobDefined);
            
            Assert.True(mockExporter.exported);
            
            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Default)));
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

    [DryJob]
    public class WithDryAttribute
    {
        [Benchmark]
        public void Method() { }
    }
    
    public class JustBenchmark
    {
        [Benchmark]
        public void Method() { }
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
