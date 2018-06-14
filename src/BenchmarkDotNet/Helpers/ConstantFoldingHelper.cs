using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Helpers
{
    public static class ConstantFoldingHelper
    {
        private static readonly double threshold = 0.2d; //nanoseconds
        
        public static Measurement[] HandleConstantFolding(IEnumerable<Measurement> measurements)
        {
            var measurementList = measurements.ToArray();
            
            var maxIdle = measurementList.Where(x => x.IterationMode == IterationMode.IdleTarget).DefaultIfEmpty().Max();
            var minTarget = measurementList.Where(x => x.IterationMode == IterationMode.MainTarget).DefaultIfEmpty().Min();
            var meanNs = measurementList.Where(x => x.IterationMode == IterationMode.Result)
                                        .Select(x => x.GetAverageNanoseconds())
                                        .Average();

            if (maxIdle.Nanoseconds > minTarget.Nanoseconds && meanNs <= threshold)
                for (int i = 0; i < measurementList.Length; i++)
                    if (measurementList[i].IterationMode == IterationMode.Result)
                        measurementList[i] = new Measurement(measurementList[i]);

            return measurementList;
        }
    }
}