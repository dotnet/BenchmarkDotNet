using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Validators
{
    public static class DotNetSdkVersionValidator
    {
        private static readonly Lazy<List<string>> cachedFrameworkSdks =
        new Lazy<List<string>>(() => GetInstalledFrameworkSdks().ToList(), true);

        public static IEnumerable<ValidationError> ValidateCoreSdks(string? customDotNetCliPath, BenchmarkCase benchmark)
        {
            if (!HasRuntimeMoniker(benchmark))
            {
                return Enumerable.Empty<ValidationError>();
            }

            var runtimeMoniker = benchmark.Job.Environment.Runtime.RuntimeMoniker;
            string requiredSdkVersion = GetSdkVersionFromMoniker(runtimeMoniker);

            var installedSdks = GetInstalledDotNetSdks(customDotNetCliPath);

            if (!installedSdks.Any(sdk => sdk.StartsWith(requiredSdkVersion)))
            {
                return new ValidationError[]
                {
                    new ValidationError(true, $"The required .NET Core SDK version {requiredSdkVersion} for runtime moniker {runtimeMoniker} is not installed.", benchmark)
                };
            }

            return Enumerable.Empty<ValidationError>();
        }

        public static IEnumerable<ValidationError> ValidateFrameworkSdks(BenchmarkCase benchmark)
        {
            if (!HasRuntimeMoniker(benchmark))
            {
                return Enumerable.Empty<ValidationError>();
            }

            var runtimeMoniker = benchmark.Job.Environment.Runtime.RuntimeMoniker;
            var requiredSdkVersion = GetSdkVersionFromMoniker(runtimeMoniker);
            if (!cachedFrameworkSdks.Value.Any(sdk => sdk.StartsWith(requiredSdkVersion)))
            {
                return new ValidationError[]
                {
                    new ValidationError(true, $"The required .NET Framework SDK version {requiredSdkVersion} for runtime moniker {runtimeMoniker} is not installed.", benchmark)
                };
            }

            return Enumerable.Empty<ValidationError>();
        }

        private static bool HasRuntimeMoniker(BenchmarkCase benchmark)
        {
            return !(benchmark == null || benchmark.Job == null || benchmark.Job.Environment?.Runtime?.RuntimeMoniker == null);
        }

        private static IEnumerable<string> GetInstalledDotNetSdks(string? customDotNetCliPath)
        {
            string dotnetExecutable = string.IsNullOrEmpty(customDotNetCliPath) ? "dotnet" : customDotNetCliPath;
            var startInfo = new ProcessStartInfo(dotnetExecutable, "--list-sdks")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return Enumerable.Empty<string>();
                    }

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        return lines.Select(line => line.Split(' ')[0]); // The SDK version is the first part of each line.
                    }
                    else
                    {
                        return Enumerable.Empty<string>();
                    }
                }
            }
            catch (Win32Exception) // dotnet CLI is not installed or not found in the path.
            {
                return Enumerable.Empty<string>();
            }
        }

        public static IEnumerable<string> GetInstalledFrameworkSdks()
        {
            var versions = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Get1To45VersionFromRegistry(versions);
                Get45PlusFromRegistry(versions);
            }

            return versions;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This code is protected with a runtime OS platform check")]
        private static void Get1To45VersionFromRegistry(List<string> versions)
        {
            using (var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\\"))
            {
                if (ndpKey == null)
                {
                    return;
                }

                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName == "v4")
                    {
                        continue;
                    }

                    if (versionKeyName.StartsWith("v"))
                    {
                        var versionKey = ndpKey.OpenSubKey(versionKeyName);
                        string name = (string)versionKey.GetValue("Version", "");
                        string sp = versionKey.GetValue("SP", "").ToString();
                        string install = versionKey.GetValue("Install", "").ToString();
                        if (string.IsNullOrEmpty(install))
                        {
                            versions.Add(name);
                        }
                        else
                        {
                            if (!(string.IsNullOrEmpty(sp)) && install == "1")
                            {
                                versions.Add(name + " SP" + sp);
                            }
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        foreach (string subKeyName in versionKey.GetSubKeyNames())
                        {
                            var subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (!string.IsNullOrEmpty(name))
                            {
                                sp = subKey.GetValue("SP", "").ToString();
                            }

                            install = subKey.GetValue("Install", "").ToString();
                            if (string.IsNullOrEmpty(install))
                            {
                                versions.Add(name);
                            }
                            else
                            {
                                if (!(string.IsNullOrEmpty(sp)) && install == "1")
                                {
                                    versions.Add(name + " SP" + sp);
                                }
                                else if (install == "1")
                                {
                                    versions.Add(name);
                                }
                            }
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This code is protected with a runtime OS platform check")]
        private static void Get45PlusFromRegistry(List<string> versions)
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subkey))
            {
                if (ndpKey == null)
                {
                    return;
                }

                if (ndpKey.GetValue("Version") != null)
                {
                    versions.Add(ndpKey.GetValue("Version").ToString());
                }
                else
                {
                    if (ndpKey.GetValue("Release") != null)
                    {
                        versions.Add(CheckFor45PlusVersion((int)ndpKey.GetValue("Release")));
                    }
                }
            }
        }

        private static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 533320)
                return "4.8.1";
            if (releaseKey >= 528040)
                return "4.8";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";

            return "";
        }

        private static string GetSdkVersionFromMoniker(RuntimeMoniker runtimeMoniker)
        {
            switch (runtimeMoniker)
            {
                case RuntimeMoniker.Net461:
                    return "4.6.1";

                case RuntimeMoniker.Net462:
                    return "4.6.2";

                case RuntimeMoniker.Net47:
                    return "4.7";

                case RuntimeMoniker.Net471:
                    return "4.7.1";

                case RuntimeMoniker.Net472:
                    return "4.7.2";

                case RuntimeMoniker.Net48:
                    return "4.8";

                case RuntimeMoniker.Net481:
                    return "4.8.1";

                case RuntimeMoniker.NetCoreApp20:
                    return "2.0";

                case RuntimeMoniker.NetCoreApp21:
                    return "2.1";

                case RuntimeMoniker.NetCoreApp22:
                    return "2.2";

                case RuntimeMoniker.NetCoreApp30:
                    return "3.0";

                case RuntimeMoniker.NetCoreApp31:
                    return "3.1";

                case RuntimeMoniker.Net50:
                    return "5.0";

                case RuntimeMoniker.Net60:
                    return "6.0";

                case RuntimeMoniker.Net70:
                    return "7.0";

                case RuntimeMoniker.Net80:
                    return "8.0";

                case RuntimeMoniker.Net90:
                    return "9.0";

                case RuntimeMoniker.NativeAot60:
                    return "6.0";

                case RuntimeMoniker.NativeAot70:
                    return "7.0";

                case RuntimeMoniker.NativeAot80:
                    return "8.0";

                case RuntimeMoniker.NativeAot90:
                    return "9.0";

                case RuntimeMoniker.Mono60:
                    return "6.0";

                case RuntimeMoniker.Mono70:
                    return "7.0";

                case RuntimeMoniker.Mono80:
                    return "8.0";

                case RuntimeMoniker.Mono90:
                    return "9.0";

                default:
                    throw new NotImplementedException($"SDK version check not implemented for {runtimeMoniker}");
            }
        }
    }
}