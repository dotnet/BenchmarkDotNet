using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace BenchmarkDotNet.Diagnosers
{
    // initial list is based on counters available for Windows, run `tracelog.exe -profilesources Help` to get the list
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [DebuggerDisplay("{Name,nq}")]
    public readonly struct HardwareCounterInfo : IEquatable<HardwareCounterInfo>
    {
        public HardwareCounterInfo(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var friendlyNameIndex = name.IndexOf('/');
            Name = friendlyNameIndex > 0 ? name.Substring(0, friendlyNameIndex) : name;
            DisplayName = friendlyNameIndex > 0 ? name.Substring(friendlyNameIndex + 1) : name;
            ShortName = DisplayName;
            TheGreaterTheBetter = true;
        }

        public HardwareCounterInfo(string name, string displayName) : this(name, displayName, displayName)
        {
        }

        public HardwareCounterInfo(string name, string displayName, string shortName, bool theGreaterTheBetter = true)
        {
            Name = name;
            DisplayName = displayName;
            ShortName = shortName;
            TheGreaterTheBetter = theGreaterTheBetter;
        }

        public readonly string Name;

        public readonly string DisplayName;

        public readonly string ShortName;

        public readonly bool TheGreaterTheBetter;

        public static readonly HardwareCounterInfo Timer = new HardwareCounterInfo("Timer", nameof(Timer), "timer");
        public static readonly HardwareCounterInfo TotalIssues = new HardwareCounterInfo("TotalIssues", nameof(TotalIssues), "issues");
        public static readonly HardwareCounterInfo BranchInstructions = new HardwareCounterInfo("BranchInstructions", nameof(BranchInstructions), "branch");
        public static readonly HardwareCounterInfo CacheMisses = new HardwareCounterInfo("CacheMisses", nameof(CacheMisses), "miss");
        public static readonly HardwareCounterInfo BranchMispredictions = new HardwareCounterInfo("BranchMispredictions", nameof(BranchInstructions), "mispred");
        public static readonly HardwareCounterInfo TotalCycles = new HardwareCounterInfo("TotalCycles", nameof(TotalCycles), "cycles");
        public static readonly HardwareCounterInfo UnhaltedCoreCycles = new HardwareCounterInfo("UnhaltedCoreCycles", nameof(UnhaltedCoreCycles), "unCoreCycles");
        public static readonly HardwareCounterInfo InstructionRetired = new HardwareCounterInfo( "InstructionRetired", nameof(InstructionRetired), "retired");
        public static readonly HardwareCounterInfo UnhaltedReferenceCycles = new HardwareCounterInfo("UnhaltedReferenceCycles", nameof(UnhaltedReferenceCycles), "unRefCycles");
        public static readonly HardwareCounterInfo LlcReference = new HardwareCounterInfo("LLCReference", nameof(LlcReference), "llcRef");
        public static readonly HardwareCounterInfo LlcMisses = new HardwareCounterInfo("LLCMisses", nameof(LlcMisses), "llcMiss");
        public static readonly HardwareCounterInfo BranchInstructionRetired = new HardwareCounterInfo("BranchInstructionRetired", nameof(BranchInstructionRetired), "branchInst");
        public static readonly HardwareCounterInfo BranchMispredictsRetired = new HardwareCounterInfo("BranchMispredictsRetired", nameof(BranchMispredictsRetired), "branchMisp");

        public bool Equals(HardwareCounterInfo other) => Name == other.Name;

        public override bool Equals(object obj) => obj is HardwareCounterInfo other && Equals(other);

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);

        public static bool operator ==(HardwareCounterInfo left, HardwareCounterInfo right) => left.Equals(right);

        public static bool operator !=(HardwareCounterInfo left, HardwareCounterInfo right) => !left.Equals(right);

        public static HardwareCounterInfo Parse(string name)
        {
            if (Enum.TryParse(name, true, out HardwareCounter counter))
            {
                return counter;
            }
            return new HardwareCounterInfo(name);
        }

        public static implicit operator HardwareCounterInfo(HardwareCounter counter)
        {
            switch (counter)
            {
                case HardwareCounter.Timer:
                    return Timer;
                case HardwareCounter.TotalIssues:
                    return TotalIssues;
                case HardwareCounter.BranchInstructions:
                    return BranchInstructions;
                case HardwareCounter.CacheMisses:
                    return CacheMisses;
                case HardwareCounter.BranchMispredictions:
                    return BranchMispredictions;
                case HardwareCounter.TotalCycles:
                    return TotalCycles;
                case HardwareCounter.UnhaltedCoreCycles:
                    return UnhaltedCoreCycles;
                case HardwareCounter.InstructionRetired:
                    return InstructionRetired;
                case HardwareCounter.UnhaltedReferenceCycles:
                    return UnhaltedReferenceCycles;
                case HardwareCounter.LlcReference:
                    return LlcReference;
                case HardwareCounter.LlcMisses:
                    return LlcMisses;
                case HardwareCounter.BranchInstructionRetired:
                    return BranchInstructionRetired;
                case HardwareCounter.BranchMispredictsRetired:
                    return BranchMispredictsRetired;
                default:
                    throw new ArgumentOutOfRangeException(nameof(counter), counter, null);
            }
        }
    }
}