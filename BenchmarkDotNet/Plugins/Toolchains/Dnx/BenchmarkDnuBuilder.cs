using System;
using BenchmarkDotNet.Plugins.Toolchains.Results;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    /// <summary>
    /// relies on a MS "dnu" command line tool (it is just Microsoft.Dnx.Tooling.dll installed with dnvm)
    /// Nuget 3 will replace dnu restore in the future: https://github.com/aspnet/dnx/issues/3216
    /// </summary>
    public class BenchmarkDnuBuilder : IBenchmarkBuilder
    {
        /// <summary>
        /// generates project.lock.json that tells compiler where to take dlls and source from
        /// </summary>
        public BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult, Benchmark benchmark)
        {
            // todo: add error logging!
            if (!DnuCommandExecutor.ExecuteCommand(generateResult.DirectoryPath, "dnu restore"))
            {
                return new BenchmarkBuildResult(generateResult, true, new Exception("dnu restore has failed"));
            }

            return new BenchmarkBuildResult(generateResult, true, null);
        }
    }
}