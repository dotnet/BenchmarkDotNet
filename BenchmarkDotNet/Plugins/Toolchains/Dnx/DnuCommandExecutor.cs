using System.Diagnostics;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    internal class DnuCommandExecutor
    {
        internal static bool ExecuteCommand(string workingDirectory, string arguments)
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