using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Reports
{
    public class SummaryStyle : ISummaryStyle
    {
        public bool PrintUnitsInHeader { get; }

        public bool PrintUnitsInContent { get; }

        public TimeUnit TimeUnit { get; }

        public SummaryStyle(bool printUnitsInHeader, bool printUnitsInContent, TimeUnit timeUnit)
        {
            PrintUnitsInHeader = printUnitsInHeader;
            PrintUnitsInContent = printUnitsInContent;
            TimeUnit = timeUnit;
        }

        public static SummaryStyle Default => new SummaryStyle(
            printUnitsInHeader: false,
            printUnitsInContent: true,
            timeUnit: null
        );

        public ISummaryStyle WithCurrentOrNewTimeUnit(TimeUnit newTimeUnit)
        {
            if (this.TimeUnit != null)
                return this;

            return new SummaryStyle(
                printUnitsInHeader: this.PrintUnitsInHeader,
                printUnitsInContent: this.PrintUnitsInContent,
                timeUnit: newTimeUnit
            );
        }
    }
}
