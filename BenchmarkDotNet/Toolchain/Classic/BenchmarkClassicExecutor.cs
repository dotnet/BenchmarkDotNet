using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Toolchain.Results;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Toolchain.Classic
{
    internal class BenchmarkClassicExecutor : IBenchmarkExecutor
    {
        private readonly Benchmark benchmark;
        private readonly IBenchmarkLogger logger;
        private bool codeAlreadyExtracted = false;

        public BenchmarkClassicExecutor(Benchmark benchmark, IBenchmarkLogger logger)
        {
            this.benchmark = benchmark;
            this.logger = logger;
        }

        public BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser)
        {
            var exeName = Path.Combine(buildResult.DirectoryPath, "Program.exe");
            var args = parameters == null ? string.Empty : parameters.ToArgs();

            if (File.Exists(exeName))
            {
                var lines = new List<string>();
                var startInfo = CreateStartInfo(exeName, args);
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.PriorityClass = ProcessPriorityClass.High;
                        process.ProcessorAffinity = new IntPtr(2);
                        string line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            logger?.WriteLine(line);
                            if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                                lines.Add(line);

                            // Wait until we know "Warmup" is happening, and then dissassemble the process
                            if (codeAlreadyExtracted == false && line.StartsWith("// Warmup") && !line.StartsWith("// Warmup (idle)"))
                            {
                                try
                                {
                                    var assembly = Assembly.GetEntryAssembly();
                                    if (assembly != null)
                                        diagnoser.Print(benchmark, process, assembly.Location, logger);
                                }
                                finally
                                {
                                    // Always set this, even if something went wrong, otherwise we will try on every run of a benchmark batch
                                    codeAlreadyExtracted = true;
                                }
                            }
                        }
                        if (process.HasExited && process.ExitCode != 0)
                        {
                            if (logger != null)
                            {
                                logger.WriteError(
                                    $"Something bad happened during the execution of {exeName}. Try to run the benchmark again using an AnyCPU application\n");
                            }
                            else
                            {
                                if (exeName.ToLowerInvariant() == "msbuild")
                                    Console.WriteLine("Build failed");
                            }
                            return new BenchmarkExecResult(true, new string[0]);
                        }
                    }
                }
                return new BenchmarkExecResult(true, lines);
            }
            return new BenchmarkExecResult(false, new string[0]);
        }

        private ProcessStartInfo CreateStartInfo(string exeName, string args)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            switch (benchmark.Task.Configuration.Runtime)
            {
                case BenchmarkRuntime.Clr:
                    start.FileName = exeName;
                    start.Arguments = args;
                    break;
                case BenchmarkRuntime.Mono:
                    start.FileName = "mono";
                    start.Arguments = exeName + " " + args;
                    break;
            }
            return start;
        }
    }
}