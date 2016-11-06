using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;

// ReSharper disable once CheckNamespace
namespace BenchmarkDotNet.Jobs
{
    public sealed class AccuracyMode : JobMode<AccuracyMode>
    {
        public static readonly Characteristic<double> MaxStdErrRelativeCharacteristic = Characteristic.Create((AccuracyMode a) => a.MaxStdErrRelative);
        public static readonly Characteristic<TimeInterval> MinIterationTimeCharacteristic = Characteristic.Create((AccuracyMode a) => a.MinIterationTime);
        public static readonly Characteristic<int> MinInvokeCountCharacteristic = Characteristic.Create((AccuracyMode a) => a.MinInvokeCount);
        public static readonly Characteristic<bool> EvaluateOverheadCharacteristic = Characteristic.Create((AccuracyMode a) => a.EvaluateOverhead);
        public static readonly Characteristic<bool> RemoveOutliersCharacteristic = Characteristic.Create((AccuracyMode a) => a.RemoveOutliers);

        // TODO: fix typo
        public static readonly Characteristic<bool> AnalyzeLaunchVarianceCharacteristic = Characteristic.Create((AccuracyMode a) => a.AnalyzeLaunchVariance);

        public double MaxStdErrRelative
        {
            get { return MaxStdErrRelativeCharacteristic[this]; }
            set { MaxStdErrRelativeCharacteristic[this] = value; }
        }
        public TimeInterval MinIterationTime
        {
            get { return MinIterationTimeCharacteristic[this]; }
            set { MinIterationTimeCharacteristic[this] = value; }
        }
        public int MinInvokeCount
        {
            get { return MinInvokeCountCharacteristic[this]; }
            set { MinInvokeCountCharacteristic[this] = value; }
        }
        public bool EvaluateOverhead
        {
            get { return EvaluateOverheadCharacteristic[this]; }
            set { EvaluateOverheadCharacteristic[this] = value; }
        }
        public bool RemoveOutliers
        {
            get { return RemoveOutliersCharacteristic[this]; }
            set { RemoveOutliersCharacteristic[this] = value; }
        }
        // TODO: fix typo
        public bool AnalyzeLaunchVariance
        {
            get { return AnalyzeLaunchVarianceCharacteristic[this]; }
            set { AnalyzeLaunchVarianceCharacteristic[this] = value; }
        }
    }
}