using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Validators
{
    public class DotNetSdkVersionValidator : IValidator
    {
        private readonly string _customDotNetCliPath;
        private readonly IDotNetSdkProvider _dotNetSdkProvider;

        public DotNetSdkVersionValidator(string customDotNetCliPath)
        {
            _customDotNetCliPath = customDotNetCliPath;
            _dotNetSdkProvider = new DotNetSdkProvider(_customDotNetCliPath);
        }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark == null || benchmark.Job == null || benchmark.Job.Environment?.Runtime?.RuntimeMoniker == null)
                {
                    continue;
                }

                var runtimeMoniker = benchmark.Job.Environment.Runtime.RuntimeMoniker;
                if (!IsSdkInstalled(runtimeMoniker))
                {
                    yield return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"The required SDK for {runtimeMoniker} is not installed",
                        benchmark);
                }
            }
        }

        private bool IsSdkInstalled(RuntimeMoniker runtimeMoniker)
        {
            string requiredSdkVersion = GetSdkVersionFromMoniker(runtimeMoniker);
            var installedSdks = _dotNetSdkProvider.GetInstalledSdks();

            // If the required SDK version is for .NET Framework
            if (requiredSdkVersion.StartsWith("4"))
            {
                return installedSdks.Any(sdk => sdk.StartsWith(requiredSdkVersion) || string.Compare(sdk, requiredSdkVersion) > 0);
            }
            else
            {
                // For .NET Core and .NET 5+
                return installedSdks.Any(sdk => sdk.StartsWith(requiredSdkVersion + ".") || sdk == requiredSdkVersion);
            }
        }

        private static readonly Dictionary<RuntimeMoniker, string> SdkVersionMap = new Dictionary<RuntimeMoniker, string>
        {
            { RuntimeMoniker.Net461, "4.6.1" },
            { RuntimeMoniker.Net462, "4.6.2" },
            { RuntimeMoniker.Net47, "4.7" },
            { RuntimeMoniker.Net471, "4.7.1" },
            { RuntimeMoniker.Net472, "4.7.2" },
            { RuntimeMoniker.Net48, "4.8" },
            { RuntimeMoniker.Net481, "4.8.1" },
            { RuntimeMoniker.NetCoreApp20, "2.0" },
            { RuntimeMoniker.NetCoreApp21, "2.1" },
            { RuntimeMoniker.NetCoreApp22, "2.2" },
            { RuntimeMoniker.NetCoreApp30, "3.0" },
            { RuntimeMoniker.NetCoreApp31, "3.1" },
            { RuntimeMoniker.Net50, "5.0" },
            { RuntimeMoniker.Net60, "6.0" },
            { RuntimeMoniker.Net70, "7.0" },
            { RuntimeMoniker.Net80, "8.0" },
            { RuntimeMoniker.Net90, "9.0" },
            { RuntimeMoniker.NativeAot60, "6.0" },
            { RuntimeMoniker.NativeAot70, "7.0" },
            { RuntimeMoniker.NativeAot80, "8.0" },
            { RuntimeMoniker.NativeAot90, "9.0" },
            { RuntimeMoniker.Mono60, "6.0" },
            { RuntimeMoniker.Mono70, "7.0" },
            { RuntimeMoniker.Mono80, "8.0" },
            { RuntimeMoniker.Mono90, "9.0" },
        };

        private string GetSdkVersionFromMoniker(RuntimeMoniker runtimeMoniker)
        {
            if (SdkVersionMap.TryGetValue(runtimeMoniker, out var version))
            {
                return version;
            }

            throw new NotImplementedException($"SDK version check not implemented for {runtimeMoniker}");
        }
    }
}