using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Reports
{
    public class SummaryTable
    {
        [PublicAPI] public Summary Summary { get; }

        public SummaryTableColumn[] Columns { get; }
        public int ColumnCount { get; }

        public string[] FullHeader { get; }
        public string[][] FullContent { get; }
        public bool[] FullContentStartOfHighlightGroup { get; }
        public bool[] FullContentStartOfLogicalGroup { get; }
        public string[][] FullContentWithHeader { get; }
        [PublicAPI] public bool[] IsDefault { get; }
        public SummaryStyle EffectiveSummaryStyle { get; }
        public bool SeparateLogicalGroups { get; }

        internal SummaryTable(Summary summary, SummaryStyle? style = null)
        {
            Summary = summary;

            if (summary.HasCriticalValidationErrors)
            {
                Columns = Array.Empty<SummaryTableColumn>();
                ColumnCount = 0;
                FullHeader = Array.Empty<string>();
                FullContent = Array.Empty<string[]>();
                FullContentStartOfHighlightGroup = Array.Empty<bool>();
                FullContentWithHeader = Array.Empty<string[]>();
                IsDefault = Array.Empty<bool>();
                return;
            }

            // Ensure we have all required data for styling
            style = style ?? summary.Style ?? SummaryStyle.Default;
            if (style.TimeUnit == null)
            {
                style = style.WithTimeUnit(TimeUnit.GetBestTimeUnit(summary.Reports.Where(r => r.ResultStatistics != null).Select(r => r.ResultStatistics.Mean)
                    .ToArray()));
            }

            if (style.SizeUnit == null)
            {
                style = style.WithSizeUnit(SizeUnit.GetBestSizeUnit(summary.Reports
                    .Where(r => r.GcStats.GetBytesAllocatedPerOperation(r.BenchmarkCase).HasValue)
                    .Select(r => r.GcStats.GetBytesAllocatedPerOperation(r.BenchmarkCase).Value)
                    .ToArray()));
            }
            EffectiveSummaryStyle = style;

            var columns = summary.GetColumns();
            ColumnCount = columns.Length;
            FullHeader = columns.Select(c => c.GetColumnTitle(style)).ToArray();

            FullContent = summary.Reports.Select(r => columns.Select(c => c.GetValue(summary, r.BenchmarkCase, style)).ToArray()).ToArray();
            IsDefault = columns.Select(c => summary.Reports.All(r => c.IsDefault(summary, r.BenchmarkCase))).ToArray();

            var highlightGroupKeys = summary.BenchmarksCases.Select(b => b.Config.Orderer.GetHighlightGroupKey(b)).ToArray();
            FullContentStartOfHighlightGroup = new bool[summary.Reports.Length];
            if (highlightGroupKeys.Distinct().Count() > 1 && FullContentStartOfHighlightGroup.Length > 0)
            {
                FullContentStartOfHighlightGroup[0] = true;
                for (int i = 1; i < summary.Reports.Length; i++)
                    FullContentStartOfHighlightGroup[i] = highlightGroupKeys[i] != highlightGroupKeys[i - 1];
            }

            var logicalGroupKeys = summary.BenchmarksCases
                .Select(b => b.Config.Orderer.GetLogicalGroupKey(summary.BenchmarksCases, b))
                .ToArray();
            FullContentStartOfLogicalGroup = new bool[summary.Reports.Length];
            if (logicalGroupKeys.Distinct().Count() > 1 && FullContentStartOfLogicalGroup.Length > 0)
            {
                FullContentStartOfLogicalGroup[0] = true;
                for (int i = 1; i < summary.Reports.Length; i++)
                    FullContentStartOfLogicalGroup[i] = logicalGroupKeys[i] != logicalGroupKeys[i - 1];
            }

            SeparateLogicalGroups = summary.Orderer.SeparateLogicalGroups;

            var full = new List<string[]> { FullHeader };
            full.AddRange(FullContent);
            FullContentWithHeader = full.ToArray();

            Columns = new SummaryTableColumn[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                bool hide = summary.ColumnHidingRules.Any(rule => rule.NeedToHide(column));
                Columns[i] = new SummaryTableColumn(this, i, column, hide);
            }
        }

        public class SummaryTableColumn
        {
            [PublicAPI] public int Index { get; }
            public string Header { get; }
            public string[] Content { get; }
            public bool NeedToShow { get; }
            public int Width { get; }
            public bool IsDefault { get; }
            public TextJustification Justify { get; }
            public IColumn OriginalColumn { get; }

            internal bool IsCommon { get; }
            internal bool WasHidden { get; }

            public SummaryTableColumn(SummaryTable table, int index, IColumn column, bool hide = false)
            {
                Index = index;
                Header = table.FullHeader[index];
                Content = table.FullContent.Select(line => line[index]).ToArray();
                Width = Math.Max(Header.Length, Content.Any() ? Content.Max(line => line.Length) : 0) + 1;
                IsDefault = table.IsDefault[index];
                OriginalColumn = column;

                Justify = column.IsNumeric ? table.EffectiveSummaryStyle.NumericColumnJustification : table.EffectiveSummaryStyle.TextColumnJustification;

                bool needToShow = column.AlwaysShow || Content.Distinct().Count() > 1;
                NeedToShow = !hide && needToShow;
                WasHidden = hide && needToShow;

                bool isCommon = !NeedToShow && !IsDefault;
                IsCommon = (!hide && isCommon) || (hide && Content.Distinct().Count() == 1);
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