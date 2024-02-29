using BenchmarkDotNet.Environments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BenchmarkDotNet.Toolchains
{
    public class DotNetSdkProvider : IDotNetSdkProvider
    {
        private readonly string _customDotNetCliPath;

        public string CustomDotNetCliPath => _customDotNetCliPath;

        public DotNetSdkProvider(string customDotNetCliPath = null)
        {
            _customDotNetCliPath = customDotNetCliPath;
        }

        public IEnumerable<string> GetInstalledDotNetSdks()
        {
            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                return Enumerable.Empty<string>();
            }

            string dotnetExecutable = string.IsNullOrEmpty(CustomDotNetCliPath) ? "dotnet" : CustomDotNetCliPath;
            var startInfo = new ProcessStartInfo(dotnetExecutable, "--list-sdks")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) return Enumerable.Empty<string>();
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Select(line => line.Split(' ')[0]); // The SDK version is the first part of each line.
            }
        }
    }
}