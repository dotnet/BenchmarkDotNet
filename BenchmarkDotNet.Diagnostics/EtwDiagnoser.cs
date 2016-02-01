using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Diagnostics
{
    public abstract class ETWDiagnoser
    {
        protected readonly string ExecutedOkayMessage = "The command completed successfully.";
        protected readonly string ProfilingFolder = "ProfilingData";

        // This is the CLR runtime provider GUID for ETW Events
        protected readonly string CLRRuntimeProvider = "{e13c0d23-ccbc-4e12-931b-d9cc2eee27e4}";

        protected readonly List<int> ProcessIdsUsedInRuns = new List<int>();

        protected string RunProcess(string fileName, string arguments)
        {
            string output = string.Empty;
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            return output;
        }

        protected string GetFileName(string prefix, Benchmark benchmark, ParameterInstances parameters = null)
        {
            if (parameters != null && parameters.Items.Count > 0)
                return $"{prefix}-{benchmark.ShortInfo}-{parameters.FullInfo}";
            return $"{prefix}-{benchmark.ShortInfo}";
        }

        protected static void DeleteIfFileExists(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }
}
