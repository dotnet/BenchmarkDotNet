using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Reports
{
    public class SummaryTable
    {
        public Summary Summary { get; }

        public SummaryTableColumn[] Columns { get; }
        public int ColumnCount { get; }

        public string[] FullHeader { get; }
        public string[][] FullContent { get; }
        public string[][] FullContentWithHeader { get; }

        internal SummaryTable(Summary summary)
        {
            Summary = summary;
            var columns = summary.Config.GetColumns().Where(c => c.IsAvailable(summary)).ToArray();
            ColumnCount = columns.Length;
            FullHeader = columns.Select(c => c.ColumnName).ToArray();

            var reports = summary.Reports.Values.
                OrderBy(r => r.Benchmark.Parameters.FullInfo).
                ThenBy(r => r.Benchmark.Target.Type.Name).
                ToList();
            FullContent = reports.Select(r => columns.Select(c => c.GetValue(summary, r.Benchmark)).ToArray()).ToArray();

            var full = new List<string[]> { FullHeader };
            full.AddRange(FullContent);
            FullContentWithHeader = full.ToArray();

            Columns = Enumerable.Range(0, columns.Length).Select(i => new SummaryTableColumn(this, i, columns[i].AlwaysShow)).ToArray();
        }

        public class SummaryTableColumn
        {
            public int Index { get; }
            public string Header { get; }
            public string[] Content { get; }
            public bool NeedToShow { get; }
            public int Width { get; }
            public bool IsTrivial { get; }

            public SummaryTableColumn(SummaryTable table, int index, bool alwaysShow)
            {
                Index = index;
                Header = table.FullHeader[index];
                Content = table.FullContent.Select(line => line[index]).ToArray();
                NeedToShow = alwaysShow || Content.Distinct().Count() > 1;
                Width = Math.Max(Header.Length, Content.Any() ? Content.Max(line => line.Length) : 0) + 1;
                IsTrivial = Header.IsOneOf("Platform", "Jit", "Framework", "Runtime", "ProcessCount", "WarmupCount", "TargetCount", "Affinity", "Toolchain") &&
                            Content.Distinct().Count() == 1 &&
                            Content.First().IsOneOf("Host", "Auto", "Classic");
            }

            public override string ToString() => Header;
        }
    }
}