using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers.Hashing;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Helpers
{
    internal static class ArtifactFileNameHelper
    {
        private const int WindowsOldPathLimit = 260;
        private const int CommonSenseLimit = 1024; // for benchmarks that use args like "new string('a', 200_000)"
        private const int MaxFileNameLength = 255;

        internal static string GetTraceFilePath(DiagnoserActionParameters details, DateTime creationTime, string fileExtension)
        {
            return GetFilePath(details, null, creationTime, fileExtension, "userheap.etl".Length - fileExtension.Length);
        }

        internal static string GetFilePath(DiagnoserActionParameters details, string? subfolder, DateTime? creationTime, string fileExtension, int reserve)
        {
            string nameNoLimit = GetFilePathNoLimits(details, subfolder, creationTime, fileExtension);

            string fileNameOnly = Path.GetFileName(nameNoLimit);

            // long paths can be enabled on Windows but it does not mean that everything is going to work fine..
            // so we always use 260 as limit on Windows
            int limit = OsDetector.IsWindows()
                ? WindowsOldPathLimit - reserve
                : CommonSenseLimit;

            if (nameNoLimit.Length <= limit && fileNameOnly.Length <= MaxFileNameLength)
            {
                return nameNoLimit;
            }

            return GetLimitedFilePath(details, subfolder, creationTime, fileExtension, limit);
        }

        private static string GetFilePathNoLimits(DiagnoserActionParameters details, string? subfolder, DateTime? creationTime, string fileExtension)
        {
            string fileName = $@"{FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type)}.{FullNameProvider.GetMethodName(details.BenchmarkCase)}";

            return GetFilePath(fileName, details, subfolder, creationTime, fileExtension);
        }

        private static string GetLimitedFilePath(DiagnoserActionParameters details, string? subfolder, DateTime? creationTime, string fileExtension, int limit)
        {
            string shortTypeName = FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type, includeNamespace: false);
            string methodName = details.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            string parameters = details.BenchmarkCase.HasParameters
                ? $"-hash{MurmurHash.HashString(FullNameProvider.GetMethodName(details.BenchmarkCase))}"
                : string.Empty;

            string fileName = $@"{shortTypeName}.{methodName}{parameters}";

            string finalResult = GetFilePath(fileName, details, subfolder, creationTime, fileExtension);

            if (finalResult.Length > limit)
            {
                throw new NotSupportedException($"The full benchmark name: \"{fileName}\" combined with artifacts path: \"{details.Config.ArtifactsPath}\" is too long. " +
                   $"Please set the value of {nameof(details.Config)}.{nameof(details.Config.ArtifactsPath)} to shorter path or rename the type or method.");
            }

            return finalResult;
        }

        private static string GetFilePath(string fileName, DiagnoserActionParameters details, string? subfolder, DateTime? creationTime, string fileExtension)
        {
            // Disambiguate output file names across the config's jobs (JobComparer has already made them distinct),
            // so a benchmark that runs under more than one job doesn't produce colliding files. If the jobs differ
            // in runtime, tag every file with its runtime; expand to the toolchain when jobs sharing a runtime
            // configure different toolchains (jobs that don't configure one resolve to the same default for a given
            // runtime); and finally fall back to the job index (always unique) when neither separates this job from
            // another (e.g. jobs differing only by toolchain settings or some other characteristic).
            var jobs = details.Config.GetJobs().ToArray();
            if (jobs.Length > 1)
            {
                bool runtimesDiffer = jobs.Select(job => job.GetRuntime()).Distinct().Count() > 1;
                bool toolchainsDiffer = jobs
                    .GroupBy(job => job.GetRuntime())
                    .Any(group => group.Select(job => job.Infrastructure.TryGetToolchain(out var toolchain) ? toolchain.ToString() : null).Distinct().Count() > 1);

                string Disambiguator(Job job)
                {
                    string result = runtimesDiffer ? $"-{job.GetRuntime()}" : string.Empty;
                    if (toolchainsDiffer && job.Infrastructure.TryGetToolchain(out var toolchain))
                        result += $"-{toolchain}";
                    return result;
                }

                string suffix = Disambiguator(details.BenchmarkCase.Job);
                fileName += suffix;

                if (jobs.Count(job => Disambiguator(job) == suffix) > 1)
                    fileName += $"-{Array.IndexOf(jobs, details.BenchmarkCase.Job)}";
            }

            if (creationTime.HasValue)
                fileName += $"-{creationTime.Value.ToString(BenchmarkRunnerClean.DateTimeFormat)}";

            fileName = FolderNameHelper.ToFolderName(fileName);

            if (!string.IsNullOrEmpty(fileExtension))
                fileName = $"{fileName}.{fileExtension}";

            return subfolder != null
                ? Path.Combine(details.Config.ArtifactsPath, subfolder, fileName)
                : Path.Combine(details.Config.ArtifactsPath, fileName);
        }
    }
}
