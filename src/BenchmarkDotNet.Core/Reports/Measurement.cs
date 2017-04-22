using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Reports
{
    /// <summary>
    /// The basic captured statistics for a benchmark.
    /// </summary>
    public struct Measurement : IComparable<Measurement>
    {
        private static readonly Measurement Error = new Measurement(-1, IterationMode.Unknown, 0, 0, 0);

        public IterationMode IterationMode { get; }

        public int LaunchIndex { get; }

        public int IterationIndex { get; }

        /// <summary>
        /// Gets the number of operations performed.
        /// </summary>
        public long Operations { get; }

        /// <summary>
        /// Gets the total number of nanoseconds it took to perform all operations.
        /// </summary>
        public double Nanoseconds { get; }

        /// <summary>
        /// Creates an instance of <see cref="Measurement"/> struct.
        /// </summary>
        /// <param name="launchIndex"></param>
        /// <param name="iterationMode"></param>
        /// <param name="iterationIndex"></param>
        /// <param name="operations">The number of operations performed.</param>
        /// <param name="nanoseconds">The total number of nanoseconds it took to perform all operations.</param>
        public Measurement(int launchIndex, IterationMode iterationMode, int iterationIndex, long operations, double nanoseconds)
        {
            IterationMode = iterationMode;
            IterationIndex = iterationIndex;
            Operations = operations;
            Nanoseconds = nanoseconds;
            LaunchIndex = launchIndex;
        }

        public string ToOutputLine()
        {
            // Usually, a benchmarks takes more than 10 iterations (rarely more than 99)
            // PadLeft(2, ' ') looks like a good trade-off between alignment and amount of characters
            string alignedIterationIndex = IterationIndex.ToString().PadLeft(2, ' ');
            return $"{IterationMode} {alignedIterationIndex}: {GetDisplayValue()}";
        }

        private string GetDisplayValue() => $"{Operations} op, {Nanoseconds.ToStr("0.00")} ns, {GetAverageTime()}";
        private string GetAverageTime() => $"{(Nanoseconds / Operations).ToTimeStr()}/op";

        /// <summary>
        /// Parses the benchmark statistics from the plain text line.
        /// 
        /// E.g. given the input <paramref name="line"/>:
        /// 
        ///     Target 1: 10 op, 1005842518 ns
        /// 
        /// Will extract the number of <see cref="Operations"/> performed and the 
        /// total number of <see cref="Nanoseconds"/> it took to perform them.
        /// </summary>
        /// <param name="logger">The logger to write any diagnostic messages to.</param>
        /// <param name="line">The line to parse.</param>
        /// <param name="processIndex"></param>
        /// <returns>An instance of <see cref="Measurement"/> if parsed successfully. <c>Null</c> in case of any trouble.</returns>
        public static Measurement Parse(ILogger logger, string line, int processIndex)
        {
            if (line != null && line.StartsWith(GcStats.ResultsLinePrefix))
                return Error;

            try
            {
                var lineSplit = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                var iterationInfo = lineSplit[0];
                var iterationInfoSplit = iterationInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var iterationMode = ParseIterationMode(iterationInfoSplit[0]);
                var iterationIndex = 0;
                int.TryParse(iterationInfoSplit[1], out iterationIndex);

                var measurementsInfo = lineSplit[1];
                var measurementsInfoSplit = measurementsInfo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var op = 1L;
                var ns = double.PositiveInfinity;
                foreach (var item in measurementsInfoSplit)
                {
                    var measurementSplit = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var value = measurementSplit[0];
                    var unit = measurementSplit[1];
                    switch (unit)
                    {
                        case "ns":
                            ns = double.Parse(value, HostEnvironmentInfo.MainCultureInfo);
                            break;
                        case "op":
                            op = long.Parse(value);
                            break;
                    }
                }
                return new Measurement(processIndex, iterationMode, iterationIndex, op, ns);
            }
            catch (Exception)
            {
                logger.WriteLineError("Parse error in the following line:");
                logger.WriteLineError(line);
                return Error;
            }
        }

        private static IterationMode ParseIterationMode(string name)
        {
            IterationMode mode;
            return Enum.TryParse(name, out mode) ? mode : IterationMode.Unknown;
        }

        public int CompareTo(Measurement other) => Nanoseconds.CompareTo(other.Nanoseconds);

        public override string ToString() => $"#{LaunchIndex}/{IterationMode} {IterationIndex}: {Operations} op, {Nanoseconds.ToTimeStr()}";
    }
}