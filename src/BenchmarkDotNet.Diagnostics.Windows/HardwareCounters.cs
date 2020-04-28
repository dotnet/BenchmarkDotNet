using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public static class HardwareCounters
    {
        public static IEnumerable<ValidationError> Validate(ValidationParameters validationParameters, bool mandatory)
        {
            if (!RuntimeInformation.IsWindows())
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
            bool hasCounterNamesNotMatching = false;

            foreach (var hardwareCounter in validationParameters.Config.GetHardwareCounters())
            {
                var counterName = hardwareCounter.Name;

                if (!availableCpuCounters.ContainsKey(counterName))
                {
                    hasCounterNamesNotMatching = true;
                    yield return new ValidationError(true, $"The counter {counterName} is not available. Please make sure you are Windows 8+ without Hyper-V");
                }
            }

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark.Job.Infrastructure.TryGetToolchain(out var toolchain) && toolchain is InProcessEmitToolchain)
                {
                    yield return new ValidationError(true, "Hardware Counters and EtwProfiler are not supported for InProcessToolchain.", benchmark);
                }
            }

            // If we have a non supported HW counter, list the available names in the last validation error.
            if (hasCounterNamesNotMatching)
            {
                yield return new ValidationError(true, $"List of available counters: {string.Join(", ", availableCpuCounters.Keys)}");
            }
        }

        internal static PreciseMachineCounter FromCounter(HardwareCounterInfo counter, Func<ProfileSourceInfo, int> intervalSelector)
        {
            var counterName = counter.Name;
            var profileSource = TraceEventProfileSources.GetInfo()[counterName]; // it can't fail, diagnoser validates that first
            return new PreciseMachineCounter(profileSource.ID, counter.DisplayName, counter, intervalSelector(profileSource));
        }

        internal static void Enable(IEnumerable<PreciseMachineCounter> counters)
        {
            TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
                counters.Select(counter => counter.ProfileSourceId).ToArray(),
                counters.Select(counter => counter.Interval).ToArray());
        }
    }
}