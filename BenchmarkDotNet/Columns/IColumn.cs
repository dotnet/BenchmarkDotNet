using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public interface IColumn
    {
        string ColumnName { get; }
        string GetValue(Summary summary, Benchmark benchmark);
        bool IsAvailable(Summary summary);
        bool AlwaysShow { get; }
    }
}
