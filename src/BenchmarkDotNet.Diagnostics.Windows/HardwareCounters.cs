using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public static class HardwareCounters
    {
        public static Func<Dictionary<string, ProfileSourceInfo>> GetProfileSources { get; set; } = TraceEventProfileSources.GetInfo;

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

            var availableCpuCounters = GetProfileSources();

            foreach (var hardwareCounter in validationParameters.Config.GetHardwareCounters())
            {
                string[] counterVariants = validationParameters.Config.HardwareCounterProvider.GetVariants(hardwareCounter).ToArray();

                if (counterVariants.Length == 0)
                {
                    yield return new ValidationError(true,
                        $"Counter {hardwareCounter} not recognized. " +
                        $"Please ensure that you are using a counter that is supported by your hardware counter provider. ");
                    continue;
                }

                foreach (string counterVariant in counterVariants)
                {
                    if (!availableCpuCounters.ContainsKey(counterVariant))
                    {
                        yield return new ValidationError(true,
                            $"The counter {counterVariant} is not available. " +
                            $"Please make sure you are Windows 8+ without Hyper-V and that you are using counter available on your machine. " +
                            $"You can get the list of available counters by running `tracelog.exe -profilesources Help`");
                    }
                }
            }

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark.Job.Infrastructure.TryGetToolchain(out var toolchain) && toolchain is InProcessEmitToolchain)
                {
                    yield return new ValidationError(true, "Hardware Counters and EtwProfiler are not supported for InProcessToolchain.", benchmark);
                }
            }
        }

        internal static IEnumerable<PreciseMachineCounter> FromCounter(HardwareCounter counter, IEnumerable<string> counterVariants,
            Func<ProfileSourceInfo, int> intervalSelector)
        {
            var profileSourceInfos = GetProfileSources();
            foreach (var counterVariant in counterVariants)
            {
                if (profileSourceInfos.TryGetValue(counterVariant, out var profileSource))
                {
                    yield return new PreciseMachineCounter(profileSource.ID, profileSource.Name, counter, intervalSelector(profileSource));
                }
            }
        }

        internal static void Enable(IEnumerable<PreciseMachineCounter> counters)
        {
            TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
                counters.Select(counter => counter.ProfileSourceId).ToArray(),
                counters.Select(counter => counter.Interval).ToArray());
        }
    }
}