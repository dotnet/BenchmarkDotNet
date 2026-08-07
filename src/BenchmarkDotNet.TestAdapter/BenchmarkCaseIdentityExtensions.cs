using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// Helpers for deriving stable identities for a BenchmarkCase. Shared by the VSTest and the
    /// Microsoft.Testing.Platform adapters, because both need identities that survive across processes.
    /// </summary>
    internal static class BenchmarkCaseIdentityExtensions
    {
        /// <summary>
        /// If an ID is not provided, a random string is used for the ID. This method will identify if randomness was
        /// used for the ID and return the Job's DisplayInfo with that randomness removed so that the same benchmark
        /// can be referenced across multiple processes.
        /// </summary>
        /// <param name="benchmarkCase">The benchmark case.</param>
        /// <returns>The benchmark case' job's DisplayInfo without randomness.</returns>
        internal static string GetUnrandomizedJobDisplayInfo(this BenchmarkCase benchmarkCase)
        {
            var jobDisplayInfo = benchmarkCase.Job.DisplayInfo;
            if (!benchmarkCase.Job.HasValue(CharacteristicObject.IdCharacteristic) &&
                benchmarkCase.Job.ResolvedId.StartsWith("Job-", StringComparison.OrdinalIgnoreCase))
            {
                // Replace Job-ABCDEF with Job
                jobDisplayInfo = "Job" + jobDisplayInfo.Substring(benchmarkCase.Job.ResolvedId.Length);
            }

            return jobDisplayInfo;
        }
    }
}
