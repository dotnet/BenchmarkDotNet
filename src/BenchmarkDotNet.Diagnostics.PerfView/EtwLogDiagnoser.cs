using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnostics.PerfView
{
    public class FileNameColumn : IColumn
    {
        private readonly Dictionary<Benchmark, string> fileName;

        public FileNameColumn(Dictionary<Benchmark, string> fileName)
        {
            this.fileName = fileName;
        }

        public string Id => typeof(FileNameColumn).FullName;

        public string ColumnName => "ETW log file";

        public bool AlwaysShow => false;

        public ColumnCategory Category => ColumnCategory.Custom;

        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "File Name";

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            if (fileName.TryGetValue(benchmark, out var val) && File.Exists(val))
                return val;
            else return "-";
        }

        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
    }
}
