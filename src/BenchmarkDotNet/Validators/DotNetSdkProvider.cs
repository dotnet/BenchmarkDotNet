using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BenchmarkDotNet.Validators
{
    public class DotNetSdkProvider : ISdkProvider
    {
        public IEnumerable<string> GetInstalledSdks()
        {
            var startInfo = new ProcessStartInfo("dotnet", "--list-sdks")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return lines.Select(line => line.Split(' ')[0]); // The SDK version is the first part of each line.
        }
    }


}
