using System;
using System.Diagnostics;
using BenchmarkDotNet.Plugins.Toolchains.Results;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    /// <summary>
    /// relies on a MS "dnu" command line tool (it is just Microsoft.Dnx.Tooling.dll installed with dnvm)
    /// requires no effort but it is hard to get detailed error information
    /// Nuget 3 will replace dnu restore in the future: https://github.com/aspnet/dnx/issues/3216
    /// </summary>
    public class BenchmarkDnuBuilder : IBenchmarkBuilder
    {
        public BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult, Benchmark benchmark)
        {
            if (!ExecuteCommand(generateResult.DirectoryPath, "dnu restore"))
            {
                return new BenchmarkBuildResult(generateResult, true, new Exception("dnu restore failed"));
            }
            if (!ExecuteCommand(generateResult.DirectoryPath, "dnu build"))
            {
                return new BenchmarkBuildResult(generateResult, true, new Exception("dnu build failed"));
            }

            return new BenchmarkBuildResult(generateResult, true, null);
        }

        private static bool ExecuteCommand(string workingDirectory, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = workingDirectory,
                    Arguments = $"/c {arguments}",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();

            process.WaitForExit(); // todo: add timeout

            return process.ExitCode <= 0;
        }
    }
}