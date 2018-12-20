using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public static class IterationInfoExtensions
    {
        [PublicAPI] public static bool IsOverhead(this IIterationInfo iterationInfo) => iterationInfo.IterationMode == IterationMode.Overhead;
        [PublicAPI] public static bool IsWorkload(this IIterationInfo iterationInfo) => iterationInfo.IterationMode == IterationMode.Workload;

        [PublicAPI] public static bool IsJittingState(this IIterationInfo iterationInfo) => iterationInfo.IterationStage == IterationStage.Jitting;
        [PublicAPI] public static bool IsPilotState(this IIterationInfo iterationInfo) => iterationInfo.IterationStage == IterationStage.Pilot;
        [PublicAPI] public static bool IsWarmupState(this IIterationInfo iterationInfo) => iterationInfo.IterationStage == IterationStage.Warmup;
        [PublicAPI] public static bool IsActualState(this IIterationInfo iterationInfo) => iterationInfo.IterationStage == IterationStage.Actual;
        [PublicAPI] public static bool IsResultState(this IIterationInfo iterationInfo) => iterationInfo.IterationStage == IterationStage.Result;
    }
}