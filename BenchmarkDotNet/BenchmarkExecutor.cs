using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Logging;

namespace BenchmarkDotNet
{
    internal class BenchmarkExecutor
    {
        public IBenchmarkLogger Logger { get; }
        public bool MonoMode { get; }

        public BenchmarkExecutor(IBenchmarkLogger logger = null, bool monoMode = false)
        {
            Logger = logger;
            MonoMode = monoMode;
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
                        Logger.WriteError($"Something bad happens during starting execution of {exeName}. Try to run becnhmark using AnyCPU application");
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
                RedirectStandardOutput = true
            };
            if (MonoMode)
            {
                start.FileName = "mono";
                start.Arguments = exeName + " " + args;
            }
            else
            {
                start.FileName = exeName;
                start.Arguments = args;
            }
            return start;
        }
    }
}