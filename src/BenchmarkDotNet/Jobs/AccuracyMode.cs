using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Characteristics;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Jobs
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class AccuracyMode : JobMode<AccuracyMode>
    {
        public static readonly Characteristic<double> MaxRelativeErrorCharacteristic = CreateCharacteristic<double>(nameof(MaxRelativeError));
        public static readonly Characteristic<TimeInterval> MaxAbsoluteErrorCharacteristic = CreateCharacteristic<TimeInterval>(nameof(MaxAbsoluteError));
        public static readonly Characteristic<TimeInterval> MinIterationTimeCharacteristic = CreateCharacteristic<TimeInterval>(nameof(MinIterationTime));
        public static readonly Characteristic<int> MinInvokeCountCharacteristic = CreateCharacteristic<int>(nameof(MinInvokeCount));
        public static readonly Characteristic<bool> EvaluateOverheadCharacteristic = CreateCharacteristic<bool>(nameof(EvaluateOverhead));
        public static readonly Characteristic<OutlierMode> OutlierModeCharacteristic = CreateCharacteristic<OutlierMode>(nameof(OutlierMode));
        public static readonly Characteristic<bool> AnalyzeLaunchVarianceCharacteristic = CreateCharacteristic<bool>(nameof(AnalyzeLaunchVariance));

        /// <summary>
        /// Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error).
        /// The default value is 0.02.
        /// <remarks>If <see cref="MaxAbsoluteError"/> is also provided, the smallest value is used as stop criteria.</remarks>
        /// </summary>
        public double MaxRelativeError
        {
            get => MaxRelativeErrorCharacteristic[this];
            set => MaxRelativeErrorCharacteristic[this] = value;
        }

        /// <summary>
        /// Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error).
        /// Doesn't have a default value.
        /// <remarks>If <see cref="MaxRelativeError"/> is also provided, the smallest value is used as stop criteria.</remarks>
        /// </summary>
        public TimeInterval MaxAbsoluteError
        {
            get => MaxAbsoluteErrorCharacteristic[this];
            set => MaxAbsoluteErrorCharacteristic[this] = value;
        }

        /// <summary>
        /// Minimum time of a single iteration. Unlike Run.IterationTime, this characteristic specifies only the lower limit. In case of need, BenchmarkDotNet can increase this value.
        /// The default value is 500 milliseconds.
        /// </summary>
        public TimeInterval MinIterationTime
        {
            get => MinIterationTimeCharacteristic[this];
            set => MinIterationTimeCharacteristic[this] = value;
        }

        /// <summary>
        /// Minimum count of benchmark invocations per iteration.
        /// The default value is 4.
        /// </summary>
        public int MinInvokeCount
        {
            get => MinInvokeCountCharacteristic[this];
            set => MinInvokeCountCharacteristic[this] = value;
        }

        /// <summary>
        /// Specifies if the overhead should be evaluated (Idle runs) and it's average value subtracted from every result.
        /// True by default, very important for nano-benchmarks.
        /// </summary>
        public bool EvaluateOverhead
        {
            get => EvaluateOverheadCharacteristic[this];
            set => EvaluateOverheadCharacteristic[this] = value;
        }

        /// <summary>
        /// Specifies which outliers should be removed from the distribution.
        /// </summary>
        public OutlierMode OutlierMode
        {
            get => OutlierModeCharacteristic[this];
            set => OutlierModeCharacteristic[this] = value;
        }

        public bool AnalyzeLaunchVariance
        {
            get => AnalyzeLaunchVarianceCharacteristic[this];
            set => AnalyzeLaunchVarianceCharacteristic[this] = value;
        }
    }
}