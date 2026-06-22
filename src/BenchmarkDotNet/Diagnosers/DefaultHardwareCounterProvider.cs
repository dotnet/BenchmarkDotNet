namespace BenchmarkDotNet.Diagnosers;

public class DefaultHardwareCounterProvider : IHardwareCounterProvider
{
    private static readonly Dictionary<HardwareCounter, string> EtwTranslations
        = new ()
        {
            { HardwareCounter.Timer, "Timer" },
            { HardwareCounter.TotalIssues, "TotalIssues" },
            { HardwareCounter.BranchInstructions, "BranchInstructions" },
            { HardwareCounter.CacheMisses, "CacheMisses" },
            { HardwareCounter.BranchMispredictions, "BranchMispredictions" },
            { HardwareCounter.TotalCycles, "TotalCycles" },
            { HardwareCounter.UnhaltedCoreCycles, "UnhaltedCoreCycles" },
            { HardwareCounter.InstructionRetired, "InstructionRetired" },
            { HardwareCounter.UnhaltedReferenceCycles, "UnhaltedReferenceCycles" },
            { HardwareCounter.LlcReference, "LLCReference" },
            { HardwareCounter.LlcMisses, "LLCMisses" },
            { HardwareCounter.BranchInstructionRetired, "BranchInstructionRetired" },
            { HardwareCounter.BranchMispredictsRetired, "BranchMispredictsRetired" }
        };

    public static readonly IHardwareCounterProvider Instance = new DefaultHardwareCounterProvider();

    public IEnumerable<string> GetVariants(HardwareCounter hardwareCounter)
    {
        if (EtwTranslations.TryGetValue(hardwareCounter, out var translation))
        {
            yield return translation;
        }
    }
}