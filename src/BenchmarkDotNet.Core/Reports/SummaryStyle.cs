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
        public bool PrintUnitsInHeader { get; set; }

        public bool PrintUnitsInContent { get; set; }

        public TimeUnit TimeUnit { get; set; }

        public static SummaryStyle Default => new SummaryStyle()
        {
            PrintUnitsInHeader = false,
            PrintUnitsInContent = true,
            TimeUnit = null
        };
    }
}
