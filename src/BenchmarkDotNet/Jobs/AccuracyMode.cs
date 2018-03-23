using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Jobs
{
    public sealed class AccuracyMode : JobMode<AccuracyMode>
    {
        public static readonly Characteristic<double> MaxRelativeErrorCharacteristic = Characteristic.Create((AccuracyMode a) => a.MaxRelativeError);
        public static readonly Characteristic<TimeInterval> MaxAbsoluteErrorCharacteristic = Characteristic.Create((AccuracyMode a) => a.MaxAbsoluteError);
        public static readonly Characteristic<TimeInterval> MinIterationTimeCharacteristic = Characteristic.Create((AccuracyMode a) => a.MinIterationTime);
        public static readonly Characteristic<int> MinInvokeCountCharacteristic = Characteristic.Create((AccuracyMode a) => a.MinInvokeCount);
        public static readonly Characteristic<bool> EvaluateOverheadCharacteristic = Characteristic.Create((AccuracyMode a) => a.EvaluateOverhead);
        public static readonly Characteristic<bool> RemoveOutliersCharacteristic = Characteristic.Create((AccuracyMode a) => a.RemoveOutliers);
        public static readonly Characteristic<bool> AnalyzeLaunchVarianceCharacteristic = Characteristic.Create((AccuracyMode a) => a.AnalyzeLaunchVariance);

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

        public bool RemoveOutliers
        {
            get => RemoveOutliersCharacteristic[this];
            set => RemoveOutliersCharacteristic[this] = value;
        }

        public bool AnalyzeLaunchVariance
        {
            get => AnalyzeLaunchVarianceCharacteristic[this];
            set => AnalyzeLaunchVarianceCharacteristic[this] = value;
        }
    }
}