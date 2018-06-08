using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Jobs
{
    public sealed class AccuracyMode : JobMode<AccuracyMode>
    {
        public static readonly Characteristic<double> MaxRelativeErrorCharacteristic = CreateCharacteristic<double>(nameof(MaxRelativeError));
        public static readonly Characteristic<TimeInterval> MaxAbsoluteErrorCharacteristic = CreateCharacteristic<TimeInterval>(nameof(MaxAbsoluteError));
        public static readonly Characteristic<TimeInterval> MinIterationTimeCharacteristic = CreateCharacteristic<TimeInterval>(nameof(MinIterationTime));
        public static readonly Characteristic<int> MinInvokeCountCharacteristic = CreateCharacteristic<int>(nameof(MinInvokeCount));
        public static readonly Characteristic<bool> EvaluateOverheadCharacteristic = CreateCharacteristic<bool>(nameof(EvaluateOverhead));
        public static readonly Characteristic<OutlierMode> OutlierModeCharacteristic = CreateCharacteristic<OutlierMode>(nameof(OutlierMode));
        public static readonly Characteristic<bool> AnalyzeLaunchVarianceCharacteristic = CreateCharacteristic<bool>(nameof(AnalyzeLaunchVariance));

        public double MaxRelativeError
        {
            get => MaxRelativeErrorCharacteristic[this];
            set => MaxRelativeErrorCharacteristic[this] = value;
        }

        public TimeInterval MaxAbsoluteError
        {
            get => MaxAbsoluteErrorCharacteristic[this];
            set => MaxAbsoluteErrorCharacteristic[this] = value;
        }

        public TimeInterval MinIterationTime
        {
            get => MinIterationTimeCharacteristic[this];
            set => MinIterationTimeCharacteristic[this] = value;
        }

        public int MinInvokeCount
        {
            get => MinInvokeCountCharacteristic[this];
            set => MinInvokeCountCharacteristic[this] = value;
        }

        public bool EvaluateOverhead
        {
            get => EvaluateOverheadCharacteristic[this];
            set => EvaluateOverheadCharacteristic[this] = value;
        }

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