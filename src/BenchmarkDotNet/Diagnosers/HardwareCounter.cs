using System;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Diagnosers
{
    // initial list is based on counters available for Windows, run `tracelog.exe -profilesources Help` to get the list
    [SuppressMessage("ReSharper", "IdentifierTypo")]
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
}