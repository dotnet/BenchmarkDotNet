using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Reports
{
    public class SummaryTable
    {
        public Summary Summary { get; }

        public SummaryTableColumn[] Columns { get; }
        public int ColumnCount { get; }

        public string[] FullHeader { get; }
        public string[][] FullContent { get; }
        public bool[] FullContentStartOfGroup { get; }
        public string[][] FullContentWithHeader { get; }

        internal SummaryTable(Summary summary)
        {
            Summary = summary;

            var configColumns = summary.Config.
                GetColumns().
                Where(c => c.IsAvailable(summary));
            var paramColumns = summary.Benchmarks.
                SelectMany(b => b.Parameters.Items.Select(item => item.Name)).
                Distinct().
                Select(name => new ParamColumn(name));
            var diagnoserColumns = summary.Config.
                GetDiagnosers().
                Where(d => d is IColumnProvider).
                Cast<IColumnProvider>().
                SelectMany(cp => cp.GetColumns);
            var columns = configColumns.Concat(paramColumns).Concat(diagnoserColumns).ToArray();

            ColumnCount = columns.Length;
            FullHeader = columns.Select(c => c.ColumnName).ToArray();

            var orderProvider = summary.Config.GetOrderProvider() ?? DefaultOrderProvider.Instance;
            FullContent = summary.Reports.Select(r => columns.Select(c => c.GetValue(summary, r.Benchmark)).ToArray()).ToArray();
            var groupKeys = summary.Benchmarks.Select(b => orderProvider.GetGroupKey(b, summary)).ToArray();
            FullContentStartOfGroup = new bool[summary.Reports.Length];

            if (groupKeys.Distinct().Count() > 1 && FullContentStartOfGroup.Length > 0)
            {
                FullContentStartOfGroup[0] = true;
                for (int i = 1; i < summary.Reports.Length; i++)
                    FullContentStartOfGroup[i] = groupKeys[i] != groupKeys[i - 1];
            }            

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
                IsTrivial = Header.IsOneOf("Platform", "Jit", "Framework", "Runtime", "LaunchCount", "WarmupCount", "TargetCount", "Affinity", "Toolchain") &&
                            Content.Distinct().Count() == 1 &&
                            Content.First().IsOneOf("Host", "Auto", "Classic");
            }

            public override string ToString() => Header;
        }
    }
}