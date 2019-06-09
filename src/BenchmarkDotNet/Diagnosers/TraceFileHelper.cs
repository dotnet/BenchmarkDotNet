using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using System;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Diagnosers
{
    internal static class TraceFileHelper
    {
        internal static FileInfo GetFilePath(BenchmarkCase benchmarkCase, ImmutableConfig config, DateTime creationTime, string fileExtension)
        {
            string fileName = $@"{FolderNameHelper.ToFolderName(benchmarkCase.Descriptor.Type)}.{FullNameProvider.GetMethodName(benchmarkCase)}";

            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net461 vs netcoreapp2.1 etc
            if (config.GetJobs().Select(job => job.GetToolchain()).Distinct().Count() > 1)
                fileName += $"-{benchmarkCase.Job.Environment.Runtime?.Name ?? benchmarkCase.Job.GetToolchain()?.Name ?? benchmarkCase.Job.Id}";

            fileName += $"-{creationTime.ToString(BenchmarkRunnerClean.DateTimeFormat)}";

            fileName = FolderNameHelper.ToFolderName(fileName);

            return new FileInfo(Path.Combine(config.ArtifactsPath, $"{fileName}{fileExtension}"));
        }

        internal static FileInfo EnsureFolderExists(this FileInfo file)
        {
            if (!file.Directory.Exists)
                file.Directory.Create();

            return file;
        }
    }
}
