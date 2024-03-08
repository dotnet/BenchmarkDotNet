using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Tests.Exporters
{
    [RankColumn, LogicalGroupColumn, BaselineColumn]
    [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
    [BenchmarkDescription(Description = "MyRenamedTestCase")]
    public class JobBaseline_MethodsJobs_WithAttribute
    {
        [Benchmark] public void Base() { }
        [Benchmark] public void Foo() { }
        [Benchmark] public void Bar() { }
    }
}
