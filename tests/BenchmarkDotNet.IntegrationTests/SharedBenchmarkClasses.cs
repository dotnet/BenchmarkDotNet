using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.IntegrationTests;

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

public class MockExporter : ExporterBase
{
    public bool exported = false;
    public override ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
    {
        exported = true;
        return new();
    }
}
