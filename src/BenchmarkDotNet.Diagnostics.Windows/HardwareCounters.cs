using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Frozen;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public static class HardwareCounters
    {
        private static readonly FrozenDictionary<HardwareCounter, string> EtwTranslations = FrozenDictionary.Create<HardwareCounter, string>(
        [
            new(HardwareCounter.Timer, "Timer"),
            new(HardwareCounter.TotalIssues, "TotalIssues"),
            new(HardwareCounter.BranchInstructions, "BranchInstructions"),
            new(HardwareCounter.CacheMisses, "CacheMisses"),
            new(HardwareCounter.BranchMispredictions, "BranchMispredictions"),
            new(HardwareCounter.TotalCycles, "TotalCycles"),
            new(HardwareCounter.UnhaltedCoreCycles, "UnhaltedCoreCycles"),
            new(HardwareCounter.InstructionRetired, "InstructionRetired"),
            new(HardwareCounter.UnhaltedReferenceCycles, "UnhaltedReferenceCycles"),
            new(HardwareCounter.LlcReference, "LLCReference"),
            new(HardwareCounter.LlcMisses, "LLCMisses"),
            new(HardwareCounter.BranchInstructionRetired, "BranchInstructionRetired"),
            new(HardwareCounter.BranchMispredictsRetired, "BranchMispredictsRetired")
        ]);

        public static IEnumerable<ValidationError> Validate(ValidationParameters validationParameters, bool mandatory)
        {
            if (!OsDetector.IsWindows())
            {
                yield return new ValidationError(true, "Hardware Counters and EtwProfiler are supported only on Windows");
                yield break;
            }

            if (!validationParameters.Config.GetHardwareCounters().Any() && mandatory)
            {
                yield return new ValidationError(true, "No Hardware Counters defined, probably a bug");
                yield break;
            }

            if (TraceEventSession.IsElevated() != true)
                yield return new ValidationError(true, "Must be elevated (Admin) to use ETW Kernel Session (required for Hardware Counters and EtwProfiler).");

            var availableCpuCounters = TraceEventProfileSources.GetInfo();

            foreach (var hardwareCounter in validationParameters.Config.GetHardwareCounters())
            {
                if (!EtwTranslations.TryGetValue(hardwareCounter, out var counterName))
                {
                    yield return new ValidationError(true,
                        $"Counter {hardwareCounter} not recognized. " +
                        $"Please make sure that you are using counter available on your machine. " +
                        $"You can get the list of available counters by running `tracelog.exe -profilesources Help`");
                    continue;
                }

                if (!availableCpuCounters.ContainsKey(counterName))
                    yield return new ValidationError(true, $"The counter {counterName} is not available. Please make sure you are Windows 8+ without Hyper-V");
            }

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark.Job.Infrastructure.TryGetToolchain(out var toolchain) && toolchain is InProcessEmitToolchain)
                {
                    yield return new ValidationError(true, "Hardware Counters and EtwProfiler are not supported for InProcessToolchain.", benchmark);
                }
            }
        }

        internal static PreciseMachineCounter FromCounter(HardwareCounter counter, Func<ProfileSourceInfo, int> intervalSelector)
        {
            var profileSource = TraceEventProfileSources.GetInfo()[EtwTranslations[counter]]; // it can't fail, diagnoser validates that first

            return new PreciseMachineCounter(profileSource.ID, profileSource.Name, counter, intervalSelector(profileSource));
        }

        internal static void Enable(IEnumerable<PreciseMachineCounter> counters)
        {
            TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
                counters.Select(counter => counter.ProfileSourceId).ToArray(),
                counters.Select(counter => counter.Interval).ToArray());
        }
    }
}
