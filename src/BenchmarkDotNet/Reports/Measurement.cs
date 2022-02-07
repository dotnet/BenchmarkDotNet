using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Reports
{
    /// <summary>
    /// The basic captured statistics for a benchmark
    /// </summary>
    public struct Measurement : IComparable<Measurement>
    {
        // We always use the same CultureInfo to simplify string conversions (ToString and Parse)
        private static readonly CultureInfo MainCultureInfo = DefaultCultureInfo.Instance;

        private const string NsSymbol = "ns";
        private const string OpSymbol = "op";

        private static Measurement Error() => new Measurement(-1, IterationMode.Unknown, IterationStage.Unknown, 0, 0, 0);

        private static readonly int IterationInfoNameMaxWidth
            = Enum.GetNames(typeof(IterationMode)).Max(text => text.Length) + Enum.GetNames(typeof(IterationStage)).Max(text => text.Length);

        public IterationMode IterationMode { get; }

        public IterationStage IterationStage { get; }

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
        /// <param name="iterationStage"></param>
        /// <param name="iterationIndex"></param>
        /// <param name="operations">The number of operations performed.</param>
        /// <param name="nanoseconds">The total number of nanoseconds it took to perform all operations.</param>
        public Measurement(int launchIndex, IterationMode iterationMode, IterationStage iterationStage, int iterationIndex, long operations, double nanoseconds)
        {
            Operations = operations;
            Nanoseconds = nanoseconds;
            LaunchIndex = launchIndex;
            IterationMode = iterationMode;
            IterationStage = iterationStage;
            IterationIndex = iterationIndex;
        }

        private static IterationMode ParseIterationMode(string name) => Enum.TryParse(name, out IterationMode mode) ? mode : IterationMode.Unknown;

        private static IterationStage ParseIterationStage(string name) => Enum.TryParse(name, out IterationStage stage) ? stage : IterationStage.Unknown;

        /// <summary>
        /// Gets the average duration of one operation.
        /// </summary>
        public TimeInterval GetAverageTime() => TimeInterval.FromNanoseconds(Nanoseconds / Operations);

        public int CompareTo(Measurement other) => Nanoseconds.CompareTo(other.Nanoseconds);

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append((IterationMode.ToString() + IterationStage).PadRight(IterationInfoNameMaxWidth, ' '));
            builder.Append(' ');

            // Usually, a benchmarks takes more than 10 iterations (rarely more than 99)
            // PadLeft(2, ' ') looks like a good trade-off between alignment and amount of characters
            builder.Append(IterationIndex.ToString(MainCultureInfo).PadLeft(2, ' '));
            builder.Append(": ");

            builder.Append(Operations.ToString(MainCultureInfo));
            builder.Append(' ');
            builder.Append(OpSymbol);
            builder.Append(", ");

            builder.Append(Nanoseconds.ToString("0.00", MainCultureInfo));
            builder.Append(' ');
            builder.Append(NsSymbol);
            builder.Append(", ");

            builder.Append(GetAverageTime().ToString(MainCultureInfo).ToAscii());
            builder.Append("/op");

            return builder.ToString();
        }

        /// <summary>
        /// Parses the benchmark statistics from the plain text line.
        ///
        /// E.g. given the input <paramref name="line"/>:
        ///
        ///     WorkloadTarget 1: 10 op, 1005842518 ns
        ///
        /// Will extract the number of <see cref="Operations"/> performed and the
        /// total number of <see cref="Nanoseconds"/> it took to perform them.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <param name="processIndex">Process launch index, indexed from one.</param>
        /// <returns>An instance of <see cref="Measurement"/> if parsed successfully. <c>Null</c> in case of any trouble.</returns>
        // ReSharper disable once UnusedParameter.Global
        public static Measurement Parse(string line, int processIndex)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(GcStats.ResultsLinePrefix))
                return Error();

            try
            {
                var lineSplit = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                string iterationInfo = lineSplit[0];
                var iterationInfoSplit = iterationInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int iterationStageIndex = 0;
                for (int i = 1; i < iterationInfoSplit[0].Length; i++)
                    if (char.IsUpper(iterationInfoSplit[0][i]))
                    {
                        iterationStageIndex = i;
                        break;
                    }

                string iterationModeStr = iterationInfoSplit[0].Substring(0, iterationStageIndex);
                string iterationStageStr = iterationInfoSplit[0].Substring(iterationStageIndex);

                var iterationMode = ParseIterationMode(iterationModeStr);
                var iterationStage = ParseIterationStage(iterationStageStr);
                int.TryParse(iterationInfoSplit[1], out int iterationIndex);

                string measurementsInfo = lineSplit[1];
                var measurementsInfoSplit = measurementsInfo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                long op = 1L;
                double ns = double.PositiveInfinity;
                foreach (string item in measurementsInfoSplit)
                {
                    var measurementSplit = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string value = measurementSplit[0];
                    string unit = measurementSplit[1];
                    switch (unit)
                    {
                        case NsSymbol:
                            ns = double.Parse(value, MainCultureInfo);
                            break;
                        case OpSymbol:
                            op = long.Parse(value, MainCultureInfo);
                            break;
                    }
                }
                return new Measurement(processIndex, iterationMode, iterationStage, iterationIndex, op, ns);
            }
            catch (Exception)
            {
#if DEBUG // some benchmarks need to write to console and when we display this error it's confusing
                Debug.WriteLine("Parse error in the following line:");
                Debug.WriteLine(line);
#endif
                return Error();
            }
        }
    }
}