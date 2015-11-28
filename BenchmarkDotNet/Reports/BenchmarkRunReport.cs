using System;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Reports
{
    /// <summary>
    /// The basic captured statistics for a benchmark.
    /// </summary>
    public sealed class BenchmarkRunReport
    {
        /// <summary>
        /// Gets the number of operations performed.
        /// </summary>
        public long Operations { get; }

        /// <summary>
        /// Gets the total number of nanoseconds it took to perform all operations.
        /// </summary>
        public double Nanoseconds { get; }

        /// <summary>
        /// Gets the number of operations performed per second (ops/sec).
        /// </summary>
        public double OpsPerSecond { get; }

        /// <summary>
        /// Gets the average duration of one operation in nanoseconds.
        /// </summary>
        public double AverageNanoseconds { get; }

        /// <summary>
        /// Creates an instance of <see cref="BenchmarkRunReport"/> class.
        /// </summary>
        /// <param name="operations">The number of operations performed.</param>
        /// <param name="nanoseconds">The total number of nanoseconds it took to perform all operations.</param>
        public BenchmarkRunReport(long operations, double nanoseconds)
        {
            Operations = operations;
            Nanoseconds = nanoseconds;
            OpsPerSecond = operations / (nanoseconds / (1000 * 1000 * 1000)); // 1,000,000,000 ns in 1 second
            AverageNanoseconds = nanoseconds / operations;
        }

        /// <summary>
        /// Parses the benchmark statistics from the plain text line.
        /// 
        /// E.g. given the input <paramref name="line"/>:
        /// 
        ///     Target 1: 10 op, 1005.8 ms, 1005842518 ns, 3332139 ticks, 100584251.7955 ns/op, 9.9 op/s
        /// 
        /// Will extract the number of <see cref="Operations"/> performed and the 
        /// total number of <see cref="Nanoseconds"/> it took to perform them.
        /// </summary>
        /// <param name="logger">The logger to write any diagnostic messages to.</param>
        /// <param name="line">The line to parse.</param>
        /// <returns>An instance of <see cref="BenchmarkRunReport"/> if parsed successfully. <c>Null</c> in case of any trouble.</returns>
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
                return new BenchmarkRunReport(op, ns);
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