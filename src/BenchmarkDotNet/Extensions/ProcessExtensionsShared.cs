using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Detectors;


namespace BenchmarkDotNet.Extensions
{
    public static partial class ProcessExtensions
    {
        private static readonly TimeSpan DefaultKillTimeout = TimeSpan.FromSeconds(30);

        public static void KillTree(this Process process) => process.KillTree(DefaultKillTimeout);

        public static void KillTree(this Process process, TimeSpan timeout)
        {
            if (OsDetector.IsWindows())
            {
                RunProcessAndIgnoreOutput("taskkill", $"/T /F /PID {process.Id}", timeout);
            }
            else
            {
                var children = new HashSet<int>();
                GetAllChildIdsUnix(process.Id, children, timeout);
                foreach (var childId in children)
                {
                    KillProcessUnix(childId, timeout);
                }
                KillProcessUnix(process.Id, timeout);
            }
        }

        private static void KillProcessUnix(int processId, TimeSpan timeout)
            => RunProcessAndIgnoreOutput("kill", $"-TERM {processId}", timeout);

        private static void GetAllChildIdsUnix(int parentId, HashSet<int> children, TimeSpan timeout)
        {
            var (exitCode, stdout) = RunProcessAndReadOutput("pgrep", $"-P {parentId}", timeout);

            if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
            {
                using (var reader = new StringReader(stdout))
                {
                    while (true)
                    {
                        var text = reader.ReadLine();
                        if (text == null)
                            return;

                        if (int.TryParse(text, out int id) && !children.Contains(id))
                        {
                            children.Add(id);
                            // Recursively get the children
                            GetAllChildIdsUnix(id, children, timeout);
                        }
                    }
                }
            }
        }

        private static (int exitCode, string output) RunProcessAndReadOutput(string fileName, string arguments, TimeSpan timeout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(startInfo))
            {
                if (process.WaitForExit((int) timeout.TotalMilliseconds))
                {
                    return (process.ExitCode, process.StandardOutput.ReadToEnd());
                }
                else
                {
                    process.Kill();
                }

                return (process.ExitCode, default);
            }
        }

        private static int RunProcessAndIgnoreOutput(string fileName, string arguments, TimeSpan timeout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (!process.WaitForExit((int) timeout.TotalMilliseconds))
                    process.Kill();

                return process.ExitCode;
            }
        }
    }
}
