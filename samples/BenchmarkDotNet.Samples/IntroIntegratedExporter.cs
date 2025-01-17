using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Exporters.IntegratedExporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples
{
    [IntegratedExporter(IntegratedExportType.HtmlExporterWithRPlotExporter)]
    public class IntroIntegratedExporter
    {
        [Benchmark]
        public void Benchmark()
        {
            var result = Calculate();
        }

        private int Calculate()
        {
            int sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                sum += i;
            }
            return sum;
        }
    }
}
