using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public struct RunResults
    {
        public List<Measurement> Idle { get; }
        public List<Measurement> Main { get; }

        public RunResults(List<Measurement> idle, List<Measurement> main)
        {
            Idle = idle;
            Main = main;
        }

        public void Print()
        {
            // TODO: use Accuracy.RemoveOutliers
            // TODO: check if resulted measurements are too small (like < 0.1ns)
            double overhead = Idle == null ? 0.0 : new Statistics(Idle.Select(m => m.Nanoseconds)).Median;
            int resultIndex = 0;
            foreach (var measurement in Main)
            {
                var resultMeasurement = new Measurement(
                    measurement.LaunchIndex,
                    IterationMode.Result,
                    ++resultIndex,
                    measurement.Operations,
                    Math.Max(0, measurement.Nanoseconds - overhead));
                WriteLine(resultMeasurement.ToOutputLine());
            }
            WriteLine();
        }

        private void WriteLine() => Console.WriteLine();
        private void WriteLine(string line) => Console.WriteLine(line);
    }
}