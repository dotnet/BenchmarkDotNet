using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Helpers 
{
    internal static class ArtifactFileNameHelper
    {
        internal static string GetFilePath(DiagnoserActionParameters details, DateTime creationTime, string fileExtension)
        {
            string fileName = $@"{FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type)}.{FullNameProvider.GetMethodName(details.BenchmarkCase)}";

            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net461 vs netcoreapp2.1 etc
            if (details.Config.GetJobs().Select(job => ToolchainExtensions.GetToolchain((Job) job)).Distinct().Count() > 1)
                fileName += $"-{details.BenchmarkCase.Job.Environment.Runtime?.Name ?? details.BenchmarkCase.GetToolchain()?.Name ?? details.BenchmarkCase.Job.Id}";

            fileName += $"-{creationTime.ToString(BenchmarkRunnerClean.DateTimeFormat)}";

            fileName = FolderNameHelper.ToFolderName(fileName);

            return Path.Combine(details.Config.ArtifactsPath, $"{fileName}.{fileExtension}");
        }
    }
}
