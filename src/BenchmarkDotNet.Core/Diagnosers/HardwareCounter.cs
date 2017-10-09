using System;

namespace BenchmarkDotNet.Diagnosers
{
    // initial list is based on counters available for Windows, run `tracelog.exe -profilesources Help` to get the list
    public enum HardwareCounter
    {
        NotSet = 0,
        Timer,
        TotalIssues,
        BranchInstructions,
        CacheMisses,
        BranchMispredictions,
        TotalCycles,
        UnhaltedCoreCycles,
        InstructionRetired,
        UnhaltedReferenceCycles,
        LlcReference,
        LlcMisses,
        BranchInstructionRetired,
        BranchMispredictsRetired
    }

    public static class HardwareCounterExtensions
    {
        public static string ToShortName(this HardwareCounter hardwareCounter)
        {
            switch (hardwareCounter)
            {
                case HardwareCounter.Timer:
                    return "timer";
                case HardwareCounter.TotalIssues:
                    return "issues";
                case HardwareCounter.BranchInstructions:
                    return "branch";
                case HardwareCounter.CacheMisses:
                    return "miss";
                case HardwareCounter.BranchMispredictions:
                    return "mispred";
                case HardwareCounter.TotalCycles:
                    return "cycles";
                case HardwareCounter.UnhaltedCoreCycles:
                    return "unCoreCycles";
                case HardwareCounter.InstructionRetired:
                    return "retired";
                case HardwareCounter.UnhaltedReferenceCycles:
                    return "unRefCycles";
                case HardwareCounter.LlcReference:
                    return "llcRef";
                case HardwareCounter.LlcMisses:
                    return "llcMiss";
                case HardwareCounter.BranchInstructionRetired:
                    return "branchInst";
                case HardwareCounter.BranchMispredictsRetired:
                    return "branchMisp";
                default:
                    throw new NotSupportedException($"{hardwareCounter} has no short name mapping");
            }
        }
    }
}