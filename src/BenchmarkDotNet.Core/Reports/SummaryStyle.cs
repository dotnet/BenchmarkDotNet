using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Reports
{
    public class SummaryStyle : ISummaryStyle
    {
        public bool PrintUnitsInHeader { get; set; }
        public bool PrintUnitsInContent { get; set; }
        public SizeUnit SizeUnit { get; set; }
        public TimeUnit TimeUnit { get; set; }

        public static SummaryStyle Default => new SummaryStyle()
        {
            PrintUnitsInHeader = false,
            PrintUnitsInContent = true,
            SizeUnit = null,
            TimeUnit = null
        };
    }
}
