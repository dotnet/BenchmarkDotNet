using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Logging;

namespace BenchmarkDotNet
{
    internal class BenchmarkExecutor
    {
        public IBenchmarkLogger Logger { get; }
        public string Runtime { get; }

        public BenchmarkExecutor(IBenchmarkLogger logger = null, string runtime = null)
        {
            Logger = logger;
            Runtime = runtime;
        }

        public IList<string> Exec(string exeName, string args = "")
        {
            var lines = new List<string>();
            var startInfo = CreateStartInfo(exeName, args);
            using (var process = Process.Start(startInfo))
                if (process != null)
                {
                    process.PriorityClass = ProcessPriorityClass.High;
                    process.ProcessorAffinity = new IntPtr(2);
                    string line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                    {
                        Logger?.WriteLine(line);
                        if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                            lines.Add(line);
                    }
                    if (process.HasExited && process.ExitCode != 0)
                    {
                        if (Logger != null)
                        {
                            Logger.WriteError(
                                $"Something bad happened during the execution of {exeName}. Try to run the benchmark again using an AnyCPU application\n");
                        }
                        else
                        {
                            if (exeName.ToLowerInvariant() == "msbuild")
                                Console.WriteLine("Build failed");
                        }
                        return new string[0];
                    }
                }
            return lines;
        }

        private ProcessStartInfo CreateStartInfo(string exeName, string args)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            if (string.IsNullOrWhiteSpace(Runtime))
            {
                start.FileName = exeName;
                start.Arguments = args;
            }
            else
            {
                start.FileName = Runtime;
                start.Arguments = exeName + " " + args;
            }
            return start;
        }
    }
}