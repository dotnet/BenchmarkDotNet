using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Extensions
{
    // we need it public to reuse it in the auto-generated dll
    // but we hide it from intellisense with following attribute
    [EditorBrowsable(EditorBrowsableState.Never)]
    [PublicAPI]
    public static class ProcessExtensions
    {
        private static readonly TimeSpan DefaultKillTimeout = TimeSpan.FromSeconds(30);

        public static void EnsureHighPriority(this Process process, ILogger logger)
        {
            try
            {
                process.PriorityClass = ProcessPriorityClass.High;
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"Failed to set up high priority. Make sure you have the right permissions. Message: {ex.Message}");
            }
        }
        
        internal static string ToPresentation(this IntPtr processorAffinity, int processorCount)
            => (RuntimeInformation.Is64BitPlatform()
                    ? Convert.ToString(processorAffinity.ToInt64(), 2)
                    : Convert.ToString(processorAffinity.ToInt32(), 2))
                .PadLeft(processorCount, '0');

        private static IntPtr FixAffinity(IntPtr processorAffinity)
        {
            int cpuMask = (1 << Environment.ProcessorCount) - 1;

            return RuntimeInformation.Is64BitPlatform()
                ? new IntPtr(processorAffinity.ToInt64() & cpuMask)
                : new IntPtr(processorAffinity.ToInt32() & cpuMask);
        }

        public static bool TrySetPriority(
            [NotNull] this Process process,
            ProcessPriorityClass priority,
            [NotNull] ILogger logger)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                process.PriorityClass = priority;
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLineError(
                    $"// ! Failed to set up priority {priority} for process {process}. Make sure you have the right permissions. Message: {ex.Message}");
            }

            return false;
        }

        public static bool TrySetAffinity(
            [NotNull] this Process process,
            IntPtr processorAffinity,
            [NotNull] ILogger logger)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                process.ProcessorAffinity = FixAffinity(processorAffinity);
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLineError(
                    $"// ! Failed to set up processor affinity 0x{(long)processorAffinity:X} for process {process}. Make sure you have the right permissions. Message: {ex.Message}");
            }

            return false;
        }

        public static IntPtr? TryGetAffinity([NotNull] this Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return process.ProcessorAffinity;
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }

        internal static void SetEnvironmentVariables(this ProcessStartInfo start, BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (benchmarkCase.Job.Environment.Runtime is ClrRuntime clrRuntime && !string.IsNullOrEmpty(clrRuntime.Version))
                start.EnvironmentVariables["COMPLUS_Version"] = clrRuntime.Version;

            if (!benchmarkCase.Job.HasValue(EnvironmentMode.EnvironmentVariablesCharacteristic))
                return;

            foreach (var environmentVariable in benchmarkCase.Job.Environment.EnvironmentVariables)
                start.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;
        }
        
        // the code below was copy-pasted from https://github.com/dotnet/cli/blob/0bc24bff775e22352c2309ef990281280f92dbaa/test/Microsoft.DotNet.Tools.Tests.Utilities/Extensions/ProcessExtensions.cs#L13

        public static void KillTree(this Process process) => process.KillTree(DefaultKillTimeout);

        public static void KillTree(this Process process, TimeSpan timeout)
        {
            string stdout;
            if (RuntimeInformation.IsWindows())
            {
                RunProcessAndWaitForExit(
                    "taskkill",
                    $"/T /F /PID {process.Id}",
                    timeout,
                    out stdout);
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

        private static void GetAllChildIdsUnix(int parentId, ISet<int> children, TimeSpan timeout)
        {
            string stdout;
            var exitCode = RunProcessAndWaitForExit(
                "pgrep",
                $"-P {parentId}",
                timeout,
                out stdout);

            if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
            {
                using (var reader = new StringReader(stdout))
                {
                    while (true)
                    {
                        var text = reader.ReadLine();
                        if (text == null)
                        {
                            return;
                        }

                        int id;
                        if (int.TryParse(text, out id))
                        {
                            children.Add(id);
                            // Recursively get the children
                            GetAllChildIdsUnix(id, children, timeout);
                        }
                    }
                }
            }
        }

        private static void KillProcessUnix(int processId, TimeSpan timeout)
        {
            string stdout;
            RunProcessAndWaitForExit(
                "kill",
                $"-TERM {processId}",
                timeout,
                out stdout);
        }

        private static int RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout, out string stdout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);

            stdout = null;
            if (process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                stdout = process.StandardOutput.ReadToEnd();
            }
            else
            {
                process.Kill();
            }

            return process.ExitCode;
        }
    }
}