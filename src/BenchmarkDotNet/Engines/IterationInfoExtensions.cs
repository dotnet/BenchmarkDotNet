using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public static class IterationInfoExtensions
    {
        [PublicAPI] public static bool IsOverhead<T>(this T iterationInfo)  where T : IIterationInfo => iterationInfo.IterationMode == IterationMode.Overhead;
        [PublicAPI] public static bool IsWorkload<T>(this T iterationInfo) where T : IIterationInfo => iterationInfo.IterationMode == IterationMode.Workload;

        [PublicAPI] public static bool IsJittingState<T>(this T iterationInfo) where T : IIterationInfo => iterationInfo.IterationStage == IterationStage.Jitting;
        [PublicAPI] public static bool IsPilotState<T>(this T iterationInfo) where T : IIterationInfo => iterationInfo.IterationStage == IterationStage.Pilot;
        [PublicAPI] public static bool IsWarmupState<T>(this T iterationInfo) where T : IIterationInfo => iterationInfo.IterationStage == IterationStage.Warmup;
        [PublicAPI] public static bool IsActualState<T>(this T iterationInfo) where T : IIterationInfo => iterationInfo.IterationStage == IterationStage.Actual;
        [PublicAPI] public static bool IsResultState<T>(this T iterationInfo) where T : IIterationInfo => iterationInfo.IterationStage == IterationStage.Result;
    }
}