using System;
using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
using Perfolizer.Horology;

// ReSharper disable MemberCanBePrivate.Global

namespace BenchmarkDotNet.Reports
{
    public class SummaryStyle : IEquatable<SummaryStyle>
    {
        public static readonly SummaryStyle Default = new SummaryStyle(DefaultCultureInfo.Instance, printUnitsInHeader: false, printUnitsInContent: true, printZeroValuesInContent: false, sizeUnit: null, timeUnit: null);
        internal const int DefaultMaxParameterColumnWidth = 15 + 5; // 5 is for postfix " [15]"

        public bool PrintUnitsInHeader { get; }
        public bool PrintUnitsInContent { get; }
        public bool PrintZeroValuesInContent { get; }
        public int MaxParameterColumnWidth { get; }
        public SizeUnit SizeUnit { get; }
        internal SizeUnit CodeSizeUnit { get; }
        public TimeUnit TimeUnit { get; }
        [NotNull]
        public CultureInfo CultureInfo { get; }

        public RatioStyle RatioStyle { get; }

        public SummaryStyle([CanBeNull] CultureInfo cultureInfo, bool printUnitsInHeader, SizeUnit sizeUnit, TimeUnit timeUnit, bool printUnitsInContent = true,
            bool printZeroValuesInContent = false, int maxParameterColumnWidth = DefaultMaxParameterColumnWidth, RatioStyle ratioStyle = RatioStyle.Value)
        {
            if (maxParameterColumnWidth < DefaultMaxParameterColumnWidth)
                throw new ArgumentOutOfRangeException(nameof(maxParameterColumnWidth), $"{DefaultMaxParameterColumnWidth} is the minimum.");

            CultureInfo = cultureInfo ?? DefaultCultureInfo.Instance;
            PrintUnitsInHeader = printUnitsInHeader;
            PrintUnitsInContent = printUnitsInContent;
            SizeUnit = sizeUnit;
            TimeUnit = timeUnit;
            PrintZeroValuesInContent = printZeroValuesInContent;
            MaxParameterColumnWidth = maxParameterColumnWidth;
            RatioStyle = ratioStyle;
            CodeSizeUnit = SizeUnit.B;
        }

        public SummaryStyle WithTimeUnit(TimeUnit timeUnit)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, timeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth, RatioStyle);

        public SummaryStyle WithSizeUnit(SizeUnit sizeUnit)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, sizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth, RatioStyle);

        public SummaryStyle WithZeroMetricValuesInContent()
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, printZeroValuesInContent: true, MaxParameterColumnWidth, RatioStyle);

        public SummaryStyle WithMaxParameterColumnWidth(int maxParameterColumnWidth)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, maxParameterColumnWidth, RatioStyle);

        public SummaryStyle WithCultureInfo(CultureInfo cultureInfo)
            => new SummaryStyle(cultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth, RatioStyle);

        public SummaryStyle WithRatioStyle(RatioStyle ratioStyle)
            => new SummaryStyle(CultureInfo, PrintUnitsInHeader, SizeUnit, TimeUnit, PrintUnitsInContent, PrintZeroValuesInContent, MaxParameterColumnWidth, ratioStyle);

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
                   && RatioStyle == other.RatioStyle;
        }

        public override bool Equals(object obj) => obj is SummaryStyle summary && Equals(summary);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = PrintUnitsInHeader.GetHashCode();
                hashCode = (hashCode * 397) ^ PrintUnitsInContent.GetHashCode();
                hashCode = (hashCode * 397) ^ PrintZeroValuesInContent.GetHashCode();
                hashCode = (hashCode * 397) ^ (SizeUnit != null ? SizeUnit.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CodeSizeUnit != null ? CodeSizeUnit.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TimeUnit != null ? TimeUnit.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MaxParameterColumnWidth;
                hashCode = (hashCode * 397) ^ RatioStyle.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SummaryStyle left, SummaryStyle right) => Equals(left, right);

        public static bool operator !=(SummaryStyle left, SummaryStyle right) => !Equals(left, right);
    }
}
