using System.Collections.Generic;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Export
{
    public interface IReportExporter
    {
        void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger);
    }
}