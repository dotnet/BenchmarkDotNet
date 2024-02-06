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
            var installedDotNetSdks = GetInstalledDotNetSdk();

            var installedFrameworkSdks = GetInstalledFrameworkSdks();

            return installedDotNetSdks.Concat(installedFrameworkSdks);
        }

        private IEnumerable<string> GetInstalledDotNetSdk()
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

        private IEnumerable<string> GetInstalledFrameworkSdks()
        {
            var versions = new List<string>();
#pragma warning disable CA1416
            using (var ndpKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\"))
            {
                foreach (var versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName.StartsWith("v"))
                    {
                        var versionKey = ndpKey.OpenSubKey(versionKeyName);
                        var version = versionKey.GetValue("Version", "").ToString();
                        var sp = versionKey.GetValue("SP", "").ToString();
                        if (!string.IsNullOrEmpty(version))
                            versions.Add(version + (string.IsNullOrEmpty(sp) ? "" : $" SP{sp}"));
                    }
                }
            }
#pragma warning restore CA1416
            return versions;
        }
    }
}