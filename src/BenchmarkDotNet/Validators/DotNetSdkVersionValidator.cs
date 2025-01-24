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
        private static readonly Lazy<List<string>> cachedFrameworkSdks = new Lazy<List<string>>(GetInstalledFrameworkSdks, true);

        public static IEnumerable<ValidationError> ValidateCoreSdks(string? customDotNetCliPath, BenchmarkCase benchmark)
        {
            if (IsCliPathInvalid(customDotNetCliPath, benchmark, out ValidationError? cliPathError))
            {
                yield return cliPathError;
            }
            else if (TryGetSdkVersion(benchmark, out Version requiredSdkVersion))
            {
                var installedSdks = GetInstalledDotNetSdks(customDotNetCliPath);
                if (!installedSdks.Any(sdk => sdk >= requiredSdkVersion))
                {
                    yield return new ValidationError(true, $"The required .NET Core SDK version {requiredSdkVersion} or higher for runtime moniker {benchmark.Job.Environment.Runtime.RuntimeMoniker} is not installed.", benchmark);
                }
            }
        }

        public static IEnumerable<ValidationError> ValidateFrameworkSdks(BenchmarkCase benchmark)
        {
            if (!TryGetSdkVersion(benchmark, out Version requiredSdkVersion))
            {
                yield break;
            }

            var installedVersionString = cachedFrameworkSdks.Value.FirstOrDefault();

            if (installedVersionString == null || Version.TryParse(installedVersionString, out var installedVersion) && installedVersion < requiredSdkVersion)
            {
                yield return new ValidationError(true, $"The required .NET Framework SDK version {requiredSdkVersion} or higher is not installed.", benchmark);
            }
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

        private static bool TryGetSdkVersion(BenchmarkCase benchmark, out Version sdkVersion)
        {
            sdkVersion = default;
            if (benchmark?.Job?.Environment?.Runtime?.RuntimeMoniker != null)
            {
                sdkVersion = GetSdkVersionFromMoniker(benchmark.Job.Environment.Runtime.RuntimeMoniker);
                return true;
            }
            return false;
        }

        private static IEnumerable<Version> GetInstalledDotNetSdks(string? customDotNetCliPath)
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
                        return Enumerable.Empty<Version>();
                    }

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd();
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

        private static Version GetSdkVersionFromMoniker(RuntimeMoniker runtimeMoniker)
        {
            return runtimeMoniker switch
            {
                RuntimeMoniker.Net461 => new Version(4, 6, 1),
                RuntimeMoniker.Net462 => new Version(4, 6, 2),
                RuntimeMoniker.Net47 => new Version(4, 7),
                RuntimeMoniker.Net471 => new Version(4, 7, 1),
                RuntimeMoniker.Net472 => new Version(4, 7, 2),
                RuntimeMoniker.Net48 => new Version(4, 8),
                RuntimeMoniker.Net481 => new Version(4, 8, 1),
                RuntimeMoniker.NetCoreApp31 => new Version(3, 1),
                RuntimeMoniker.Net50 => new Version(5, 0),
                RuntimeMoniker.Net60 => new Version(6, 0),
                RuntimeMoniker.Net70 => new Version(7, 0),
                RuntimeMoniker.Net80 => new Version(8, 0),
                RuntimeMoniker.Net90 => new Version(9, 0),
                RuntimeMoniker.Net10_0 => new Version(10, 0),
                RuntimeMoniker.NativeAot60 => new Version(6, 0),
                RuntimeMoniker.NativeAot70 => new Version(7, 0),
                RuntimeMoniker.NativeAot80 => new Version(8, 0),
                RuntimeMoniker.NativeAot90 => new Version(9, 0),
                RuntimeMoniker.NativeAot10_0 => new Version(10, 0),
                RuntimeMoniker.Mono60 => new Version(6, 0),
                RuntimeMoniker.Mono70 => new Version(7, 0),
                RuntimeMoniker.Mono80 => new Version(8, 0),
                RuntimeMoniker.Mono90 => new Version(9, 0),
                RuntimeMoniker.Mono10_0 => new Version(10, 0),
                RuntimeMoniker.Wasm => Portability.RuntimeInformation.IsNetCore && CoreRuntime.TryGetVersion(out var version) ? version : new Version(5, 0),
                RuntimeMoniker.WasmNet50 => new Version(5, 0),
                RuntimeMoniker.WasmNet60 => new Version(6, 0),
                RuntimeMoniker.WasmNet70 => new Version(7, 0),
                RuntimeMoniker.WasmNet80 => new Version(8, 0),
                RuntimeMoniker.WasmNet90 => new Version(9, 0),
                RuntimeMoniker.WasmNet10_0 => new Version(10, 0),
                RuntimeMoniker.MonoAOTLLVM => Portability.RuntimeInformation.IsNetCore && CoreRuntime.TryGetVersion(out var version) ? version : new Version(6, 0),
                RuntimeMoniker.MonoAOTLLVMNet60 => new Version(6, 0),
                RuntimeMoniker.MonoAOTLLVMNet70 => new Version(7, 0),
                RuntimeMoniker.MonoAOTLLVMNet80 => new Version(8, 0),
                RuntimeMoniker.MonoAOTLLVMNet90 => new Version(9, 0),
                RuntimeMoniker.MonoAOTLLVMNet10_0 => new Version(10, 0),
                _ => throw new NotImplementedException($"SDK version check not implemented for {runtimeMoniker}")
            };
        }
    }
}