using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#nullable enable

namespace BenchmarkDotNet.Validators
{
    internal static class DotNetSdkValidator
    {
        private static readonly Lazy<List<string>> cachedFrameworkSdks = new Lazy<List<string>>(GetInstalledFrameworkSdks, true);

        public static IEnumerable<ValidationError> ValidateCoreSdks(string? customDotNetCliPath, BenchmarkCase benchmark)
        {
            if (IsCliPathInvalid(customDotNetCliPath, benchmark, out ValidationError? cliPathError))
            {
                yield return cliPathError;
                yield break;
            }
            var requiredSdkVersion = benchmark.GetRuntime().RuntimeMoniker.GetRuntimeVersion();
            if (!GetInstalledDotNetSdks(customDotNetCliPath).Any(sdk => sdk >= requiredSdkVersion))
            {
                yield return new ValidationError(true, $"The required .NET Core SDK version {requiredSdkVersion} or higher for runtime moniker {benchmark.Job.Environment.Runtime!.RuntimeMoniker} is not installed.", benchmark);
            }
        }

        public static IEnumerable<ValidationError> ValidateFrameworkSdks(BenchmarkCase benchmark)
        {
            var targetRuntime = benchmark.Job.Environment.HasValue(EnvironmentMode.RuntimeCharacteristic)
                ? benchmark.Job.Environment.Runtime!
                : ClrRuntime.GetTargetOrCurrentVersion(benchmark.Descriptor.WorkloadMethod.DeclaringType!.Assembly);
            var requiredSdkVersion = targetRuntime.RuntimeMoniker.GetRuntimeVersion();

            var installedVersionString = cachedFrameworkSdks.Value.FirstOrDefault();
            if (installedVersionString == null || Version.TryParse(installedVersionString, out var installedVersion) && installedVersion < requiredSdkVersion)
            {
                yield return new ValidationError(true, $"The required .NET Framework SDK version {requiredSdkVersion} or higher is not installed.", benchmark);
            }
        }

        public static bool IsCliPathInvalid(string? customDotNetCliPath, BenchmarkCase benchmarkCase, [NotNullWhen(true)] out ValidationError? validationError)
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

        private static IEnumerable<Version> GetInstalledDotNetSdks(string? customDotNetCliPath)
        {
            string dotnetExecutable = string.IsNullOrEmpty(customDotNetCliPath) ? "dotnet" : customDotNetCliPath!;
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
                        return Enumerable.Empty<Version>();
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        var versions = new List<Version>(lines.Count());
                        foreach (var line in lines)
                        {
                            // Version.TryParse does not handle things like 3.0.0-WORD, so this will get just the 3.0.0 part
                            var parsableVersionPart = CoreRuntime.GetParsableVersionPart(line);
                            if (Version.TryParse(parsableVersionPart, out var version))
                            {
                                versions.Add(version);
                            }
                        }

                        return versions;
                    }
                    else
                    {
                        return Enumerable.Empty<Version>();
                    }
                }
            }
            catch (Win32Exception) // dotnet CLI is not installed or not found in the path.
            {
                return Enumerable.Empty<Version>();
            }
        }

        public static List<string> GetInstalledFrameworkSdks()
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
                    versions.Add(ndpKey.GetValue("Version")!.ToString()!);
                }
                else
                {
                    if (ndpKey.GetValue("Release") != null)
                    {
                        versions.Add(CheckFor45PlusVersion((int)ndpKey.GetValue("Release")!));
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
    }
}