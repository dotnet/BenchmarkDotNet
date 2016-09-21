using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Jobs
{
    public sealed class AccuracyMode
    {
        public static readonly AccuracyMode Default = new AccuracyMode();

        private AccuracyMode()
        {
        }

        private static ICharacteristic<T> Create<T>(string id) => Characteristic<T>.Create("Accuracy", id);

        public ICharacteristic<double> MaxStdErrRelative { get; private set; } = Create<double>(nameof(MaxStdErrRelative));
        public ICharacteristic<TimeInterval> MinIterationTime { get; private set; } = Create<TimeInterval>(nameof(MinIterationTime));
        public ICharacteristic<int> MinInvokeCount { get; private set; } = Create<int>(nameof(MinInvokeCount));
        public ICharacteristic<bool> EvaluateOverhead { get; private set; } = Create<bool>(nameof(EvaluateOverhead));
        public ICharacteristic<bool> RemoveOutliers { get; private set; } = Create<bool>(nameof(RemoveOutliers));
        public ICharacteristic<bool> AnaylyzeLaunchVariance { get; private set; } = Create<bool>(nameof(AnaylyzeLaunchVariance));

        public static AccuracyMode Parse(CharacteristicSet set)
        {
            var mode = new AccuracyMode();
            mode.MaxStdErrRelative = mode.MaxStdErrRelative.Mutate(set);
            mode.MinIterationTime = mode.MinIterationTime.Mutate(set);
            mode.MinInvokeCount = mode.MinInvokeCount.Mutate(set);
            mode.EvaluateOverhead = mode.EvaluateOverhead.Mutate(set);
            mode.RemoveOutliers = mode.RemoveOutliers.Mutate(set);
            mode.AnaylyzeLaunchVariance = mode.AnaylyzeLaunchVariance.Mutate(set);
            return mode;
        }

        public CharacteristicSet ToSet() => new CharacteristicSet(
            MaxStdErrRelative,
            MinIterationTime,
            MinInvokeCount,
            EvaluateOverhead,
            RemoveOutliers,
            AnaylyzeLaunchVariance
        );
    }
}