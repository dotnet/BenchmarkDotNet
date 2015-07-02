using System;
using BenchmarkDotNet.Logging;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkRunReport
    {
        public long Operations { get; }
        public BenchmarkTimeSpan Time { get; }

        public BenchmarkRunReport(long operations, BenchmarkTimeSpan time)
        {
            Operations = operations;
            Time = time;
        }

        public static BenchmarkRunReport Parse(IBenchmarkLogger logger, string line)
        {
            try
            {
                var op = 1L;
                var ns = double.PositiveInfinity;
                var items = line.
                    Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries)[1].
                    Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    var split = item.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    var unit = split[1];
                    switch (unit)
                    {
                        case "ns":
                            ns = double.Parse(split[0], EnvironmentHelper.MainCultureInfo);
                            break;
                        case "op":
                            op = long.Parse(split[0]);
                            break;
                    }
                }
                return new BenchmarkRunReport(op, new BenchmarkTimeSpan(ns));
            }
            catch (Exception)
            {
                logger.WriteLineError("Parse error in the following line:");
                logger.WriteLineError(line);
                return null;
            }
        }
    }
}