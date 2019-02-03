using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Horology;
// ReSharper disable MemberCanBePrivate.Global

namespace BenchmarkDotNet.Reports
{
    public class SummaryStyle : IEquatable<SummaryStyle>
    {
        public static readonly SummaryStyle Default = new SummaryStyle(printUnitsInHeader: false, printUnitsInContent: true, sizeUnit: null, timeUnit: null);

        public bool PrintUnitsInHeader { get; }
        public bool PrintUnitsInContent { get; }
        public SizeUnit SizeUnit { get; }
        public TimeUnit TimeUnit { get; }

        public SummaryStyle(bool printUnitsInHeader, SizeUnit sizeUnit, TimeUnit timeUnit, bool printUnitsInContent = true)
        {
            PrintUnitsInHeader = printUnitsInHeader;
            PrintUnitsInContent = printUnitsInContent;
            SizeUnit = sizeUnit;
            TimeUnit = timeUnit;
        }

        public SummaryStyle WithTimeUnit(TimeUnit timeUnit)
            => new SummaryStyle(PrintUnitsInHeader, SizeUnit, timeUnit, PrintUnitsInContent);

        public SummaryStyle WithSizeUnit(SizeUnit sizeUnit)
            => new SummaryStyle(PrintUnitsInHeader, sizeUnit, TimeUnit, PrintUnitsInContent);

        public bool Equals(SummaryStyle other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return PrintUnitsInHeader == other.PrintUnitsInHeader && PrintUnitsInContent == other.PrintUnitsInContent && Equals(SizeUnit, other.SizeUnit) && Equals(TimeUnit, other.TimeUnit);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((SummaryStyle) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = PrintUnitsInHeader.GetHashCode();
                hashCode = (hashCode * 397) ^ PrintUnitsInContent.GetHashCode();
                hashCode = (hashCode * 397) ^ (SizeUnit != null ? SizeUnit.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TimeUnit != null ? TimeUnit.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(SummaryStyle left, SummaryStyle right) => Equals(left, right);

        public static bool operator !=(SummaryStyle left, SummaryStyle right) => !Equals(left, right);
    }
}
