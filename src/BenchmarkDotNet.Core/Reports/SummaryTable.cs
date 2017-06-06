using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Horology;
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
        public bool[] FullContentStartOfGroup { get; }
        public string[][] FullContentWithHeader { get; }
        public bool[] IsDefault { get; }
        public ISummaryStyle EffectiveSummaryStyle { get; }

        internal SummaryTable(Summary summary, ISummaryStyle style = null)
        {
            Summary = summary;

            if (summary.HasCriticalValidationErrors)
            {
                Columns = Array.Empty<SummaryTableColumn>();
                ColumnCount = 0;
                FullHeader = Array.Empty<string>();
                FullContent = Array.Empty<string[]>();
                FullContentStartOfGroup = Array.Empty<bool>();
                FullContentWithHeader = Array.Empty<string[]>();
                IsDefault = Array.Empty<bool>();
                return;
            }
            
            // Ensure we have all required data for styling
            style = style ?? SummaryStyle.Default;
            if (style.TimeUnit == null)
            {
                style = style.WithTimeUnit(TimeUnit.GetBestTimeUnit(summary.Reports.Where(r => r.ResultStatistics != null).Select(r => r.ResultStatistics.Mean).ToArray()));
            }
            if (style.SizeUnit == null)
            {
                style = style.WithSizeUnit(SizeUnit.GetBestSizeUnit(summary.Reports.Select(r => r.GcStats.BytesAllocatedPerOperation).ToArray()));
            }

            var columns = summary.GetColumns();
            ColumnCount = columns.Length;
            FullHeader = columns.Select(c => c.GetColumnTitle(style)).ToArray();

            var orderProvider = summary.Config.GetOrderProvider() ?? DefaultOrderProvider.Instance;
            FullContent = summary.Reports.Select(r => columns.Select(c => c.GetValue(summary, r.Benchmark, style)).ToArray()).ToArray();
            IsDefault = columns.Select(c => summary.Reports.All(r => c.IsDefault(summary, r.Benchmark))).ToArray();
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

            Columns = Enumerable.Range(0, columns.Length).Select(i => new SummaryTableColumn(this, i, columns[i])).ToArray();
            EffectiveSummaryStyle = style;
        }

        public class SummaryTableColumn
        {
            public int Index { get; }
            public string Header { get; }
            public string[] Content { get; }
            public bool NeedToShow { get; }
            public int Width { get; }
            public bool IsDefault { get; }
            public TextJustification Justify { get; }
            public IColumn OriginalColumn { get; }

            public SummaryTableColumn(SummaryTable table, int index, IColumn column)
            {
                Index = index;
                Header = table.FullHeader[index];
                Content = table.FullContent.Select(line => line[index]).ToArray();
                NeedToShow = column.AlwaysShow || Content.Distinct().Count() > 1;
                Width = Math.Max(Header.Length, Content.Any() ? Content.Max(line => line.Length) : 0) + 1;
                IsDefault = table.IsDefault[index];
                OriginalColumn = column;

                Justify = column.IsNumeric ? TextJustification.Right : TextJustification.Left;
            }

            public override string ToString() => Header;

            public enum TextJustification
            {
                Left,
                Right
            }
        }
    }
}