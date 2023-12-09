using System;
using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using static BenchmarkDotNet.Reports.SummaryTable.SummaryTableColumn;

// ReSharper disable MemberCanBePrivate.Global

namespace BenchmarkDotNet.Reports
{
    public class SummaryStyle : IEquatable<SummaryStyle>
    {
        public static readonly SummaryStyle Default = new SummaryStyle(DefaultCultureInfo.Instance, printUnitsInHeader: false, printUnitsInContent: true,
            printZeroValuesInContent: false, sizeUnit: null, timeUnit: null);

        internal const int DefaultMaxParameterColumnWidth = 15 + 5; // 5 is for postfix " [15]"

        public bool PrintUnitsInHeader { get; }
        public bool PrintUnitsInContent { get; }
        public bool PrintZeroValuesInContent { get; }
        public int MaxParameterColumnWidth { get; }
        public SizeUnit? SizeUnit { get; }
        internal SizeUnit CodeSizeUnit { get; }
        public TimeUnit? TimeUnit { get; }
        public CultureInfo CultureInfo { get; }

        public RatioStyle RatioStyle { get; }

        public TextJustification TextColumnJustification { get; }
        public TextJustification NumericColumnJustification { get; }

        public SummaryStyle(CultureInfo? cultureInfo, bool printUnitsInHeader, SizeUnit? sizeUnit, TimeUnit? timeUnit, bool printUnitsInContent = true,
            bool printZeroValuesInContent = false, int maxParameterColumnWidth = DefaultMaxParameterColumnWidth, RatioStyle ratioStyle = RatioStyle.Value,
            TextJustification textColumnJustification = TextJustification.Left, TextJustification numericColumnJustification = TextJustification.Right)
        {
            if (maxParameterColumnWidth < DefaultMaxParameterColumnWidth)
                throw new ArgumentOutOfRangeException(nameof(maxParameterColumnWidth), $"{DefaultMaxParameterColumnWidth} is the minimum.");

            CultureInfo = cultureInfo ?? DefaultCultureInfo.Instance;
            PrintUnitsInHeader = printUnitsInHeader;
            PrintUnitsInContent = printUnitsInContent;
            SizeUnit = sizeUnit;
            TimeUnit = timeUnit;
            MaxParameterColumnWidth = maxParameterColumnWidth;
            RatioStyle = ratioStyle;
            CodeSizeUnit = SizeUnit.B;
            PrintZeroValuesInContent = printZeroValuesInContent;
            TextColumnJustification = textColumnJustification;
            NumericColumnJustification = numericColumnJustification;
        }

        public SummaryStyle WithTimeUnit(TimeUnit timeUnit)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, timeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth,
                RatioStyle, TextColumnJustification, NumericColumnJustification);

        public SummaryStyle WithSizeUnit(SizeUnit sizeUnit)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, sizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth,
                RatioStyle, TextColumnJustification, NumericColumnJustification);

        public SummaryStyle WithZeroMetricValuesInContent()
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, printZeroValuesInContent: true,
                MaxParameterColumnWidth, RatioStyle, TextColumnJustification, NumericColumnJustification);

        public SummaryStyle WithMaxParameterColumnWidth(int maxParameterColumnWidth)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, maxParameterColumnWidth,
                RatioStyle, TextColumnJustification, NumericColumnJustification);

        public SummaryStyle WithCultureInfo(CultureInfo cultureInfo)
            => new SummaryStyle(cultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth,
                RatioStyle, TextColumnJustification, NumericColumnJustification);

        public SummaryStyle WithRatioStyle(RatioStyle ratioStyle)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth,
                ratioStyle, TextColumnJustification, NumericColumnJustification);

        public bool Equals(SummaryStyle other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(CultureInfo, other.CultureInfo)
                   && PrintUnitsInHeader == other.PrintUnitsInHeader
                   && PrintUnitsInContent == other.PrintUnitsInContent
                   && PrintZeroValuesInContent == other.PrintZeroValuesInContent
                   && Equals(SizeUnit, other.SizeUnit)
                   && Equals(CodeSizeUnit, other.CodeSizeUnit)
                   && Equals(TimeUnit, other.TimeUnit)
                   && MaxParameterColumnWidth == other.MaxParameterColumnWidth
                   && RatioStyle == other.RatioStyle
                   && TextColumnJustification == other.TextColumnJustification
                   && NumericColumnJustification == other.NumericColumnJustification;
        }

        public override bool Equals(object obj) => obj is SummaryStyle summary && Equals(summary);

        public override int GetHashCode() =>
            HashCode.Combine(
                PrintUnitsInHeader,
                PrintUnitsInContent,
                PrintZeroValuesInContent,
                SizeUnit,
                CodeSizeUnit,
                TimeUnit,
                MaxParameterColumnWidth,
                RatioStyle,
                TextColumnJustification,
                NumericColumnJustification);

        public static bool operator ==(SummaryStyle left, SummaryStyle right) => Equals(left, right);

        public static bool operator !=(SummaryStyle left, SummaryStyle right) => !Equals(left, right);
    }
}