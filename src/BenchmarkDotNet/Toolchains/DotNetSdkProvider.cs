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

        public IEnumerable<string> GetInstalledSdks()
        {
            var installedDotNetSdks = GetInstalledDotNetSdk();
            var installedFrameworkSdks = GetInstalledFrameworkSdks();
            return installedDotNetSdks.Concat(installedFrameworkSdks);
        }

        private IEnumerable<string> GetInstalledDotNetSdk()
        {
            string dotnetExecutable = string.IsNullOrEmpty(CustomDotNetCliPath) ? "dotnet" : CustomDotNetCliPath;
            var startInfo = new ProcessStartInfo(dotnetExecutable, "--list-sdks")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) throw new InvalidOperationException("Failed to start the dotnet process.");
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Select(line => line.Split(' ')[0]); // The SDK version is the first part of each line.
            }
        }

        private IEnumerable<string> GetInstalledFrameworkSdks()
        {
            var versions = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
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
            }

            return versions;
        }
    }
}
