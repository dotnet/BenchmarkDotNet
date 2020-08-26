using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Helpers
{
    internal static class ArtifactFileNameHelper
    {
        private const int WindowsOldPathLimit = 260;
        private const int CommonSenseLimit = 1024; // for benchmarks that use args like "new string('a', 200_000)"

        internal static string GetTraceFilePath(DiagnoserActionParameters details, DateTime creationTime, string fileExtension)
        {
            string nameNoLimit = GetFilePathNoLimits(details, creationTime, fileExtension);

            int limit = PathFeatures.AreAllLongPathsAvailable() ? CommonSenseLimit : WindowsOldPathLimit;

            if (nameNoLimit.Length <= limit)
            {
                return nameNoLimit;
            }

            return GetLimitedFilePath(details, creationTime, fileExtension, limit);
        }

        private static string GetFilePathNoLimits(DiagnoserActionParameters details, DateTime creationTime, string fileExtension)
        {
            string fileName = $@"{FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type)}.{FullNameProvider.GetMethodName(details.BenchmarkCase)}";

            return GetFilePath(fileName, details, creationTime, fileExtension);
        }

        private static string GetLimitedFilePath(DiagnoserActionParameters details, DateTime creationTime, string fileExtension, int limit)
        {
            string shortTypeName = FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type, includeNamespace: false);
            string methodName = details.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            string parameters = details.BenchmarkCase.HasParameters
                ? $"-hash{Hashing.HashString(FullNameProvider.GetMethodName(details.BenchmarkCase)).ToString()}"
                : string.Empty;

            string fileName = $@"{shortTypeName}.{methodName}{parameters}";

            string finalResult = GetFilePath(fileName, details, creationTime, fileExtension);

            if (finalResult.Length > limit)
            {
                throw new NotSupportedException($"The full benchmark name: \"{fileName}\" combined with artifiacts path: \"{details.Config.ArtifactsPath}\" is too long. " +
                   $"Please enable long paths support, set the value of {nameof(details.Config)}.{nameof(details.Config.ArtifactsPath)} to shorter path or rename the type or method.");
            }

            return finalResult;
        }

        private static string GetFilePath(string fileName, DiagnoserActionParameters details, DateTime creationTime, string fileExtension)
        {
            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net461 vs netcoreapp2.1 etc
            if (details.Config.GetJobs().Select(job => ToolchainExtensions.GetToolchain(job)).Distinct().Count() > 1)
                fileName += $"-{details.BenchmarkCase.Job.Environment.Runtime?.Name ?? details.BenchmarkCase.GetToolchain()?.Name ?? details.BenchmarkCase.Job.Id}";

            fileName += $"-{creationTime.ToString(BenchmarkRunnerClean.DateTimeFormat)}";

            fileName = FolderNameHelper.ToFolderName(fileName);

            return Path.Combine(details.Config.ArtifactsPath, $"{fileName}.{fileExtension}");
        }
    }
}
