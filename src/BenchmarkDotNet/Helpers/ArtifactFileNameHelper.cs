using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
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
            return GetFilePath(details, null, creationTime, fileExtension, "userheap.etl".Length);
        }

        internal static string GetFilePath(DiagnoserActionParameters details, string? subfolder, DateTime? creationTime, string fileExtension, int reserve)
        {
            string nameNoLimit = GetFilePathNoLimits(details, subfolder, creationTime, fileExtension);

            // long paths can be enabled on Windows but it does not mean that everything is going to work fine..
            // so we always use 260 as limit on Windows
            int limit =  RuntimeInformation.IsWindows()
                ? WindowsOldPathLimit - reserve
                : CommonSenseLimit;

            if (nameNoLimit.Length <= limit)
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
                ? $"-hash{Hashing.HashString(FullNameProvider.GetMethodName(details.BenchmarkCase))}"
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
            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net462 vs netcoreapp2.1 etc
            if (details.Config.GetJobs().Select(job => ToolchainExtensions.GetToolchain(job)).Distinct().Count() > 1)
                fileName += $"-{details.BenchmarkCase.Job.Environment.Runtime?.Name ?? details.BenchmarkCase.GetToolchain()?.Name ?? details.BenchmarkCase.Job.Id}";

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
