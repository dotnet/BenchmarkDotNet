using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Validators
{
    internal static class DotNetSdkValidator
    {
        private static readonly Lazy<IEnumerable<string>> cachedFrameworkSdks = new Lazy<IEnumerable<string>>(GetInstalledFrameworkSdks, true);

        public static IEnumerable<ValidationError> ValidateCoreSdks(string? customDotNetCliPath, BenchmarkCase benchmark)
        {
            if (IsCliPathInvalid(customDotNetCliPath, benchmark, out ValidationError? cliPathError))
            {
                return new ValidationError[] { cliPathError };
            }

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

        public static bool IsCliPathInvalid(string customDotNetCliPath, BenchmarkCase benchmarkCase, out ValidationError? validationError)
        {
            validationError = null;

            if (string.IsNullOrEmpty(customDotNetCliPath) && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                validationError = new ValidationError(true,
                    $"BenchmarkDotNet requires dotnet SDK to be installed or path to local dotnet cli provided in explicit way using `--cli` argument, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);

                return true;
            }

            if (!string.IsNullOrEmpty(customDotNetCliPath) && !File.Exists(customDotNetCliPath))
            {
                validationError = new ValidationError(true,
                    $"Provided custom dotnet cli path does not exist, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);

                return true;
            }

            return false;
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
                Get45PlusFromRegistry(versions);
            }

            return versions;
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
            return runtimeMoniker switch
            {
                RuntimeMoniker.Net461 => "4.6.1",
                RuntimeMoniker.Net462 => "4.6.2",
                RuntimeMoniker.Net47 => "4.7",
                RuntimeMoniker.Net471 => "4.7.1",
                RuntimeMoniker.Net472 => "4.7.2",
                RuntimeMoniker.Net48 => "4.8",
                RuntimeMoniker.Net481 => "4.8.1",
                RuntimeMoniker.NetCoreApp20 => "2.0",
                RuntimeMoniker.NetCoreApp21 => "2.1",
                RuntimeMoniker.NetCoreApp22 => "2.2",
                RuntimeMoniker.NetCoreApp30 => "3.0",
                RuntimeMoniker.NetCoreApp31 => "3.1",
                RuntimeMoniker.Net50 => "5.0",
                RuntimeMoniker.Net60 => "6.0",
                RuntimeMoniker.Net70 => "7.0",
                RuntimeMoniker.Net80 => "8.0",
                RuntimeMoniker.Net90 => "9.0",
                RuntimeMoniker.NativeAot60 => "6.0",
                RuntimeMoniker.NativeAot70 => "7.0",
                RuntimeMoniker.NativeAot80 => "8.0",
                RuntimeMoniker.NativeAot90 => "9.0",
                RuntimeMoniker.Mono60 => "6.0",
                RuntimeMoniker.Mono70 => "7.0",
                RuntimeMoniker.Mono80 => "8.0",
                RuntimeMoniker.Mono90 => "9.0",
                _ => throw new NotImplementedException($"SDK version check not implemented for {runtimeMoniker}")
            };
        }
    }
}