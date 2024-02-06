using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Validators
{
    public class SdkValidator : IValidator
    {
        private readonly ISdkProvider sdkProvider;

        public static readonly SdkValidator Instance = new SdkValidator(new DotNetSdkProvider());

        public SdkValidator(ISdkProvider sdkProvider)
        {
            this.sdkProvider = sdkProvider;
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
            var installedSdks = sdkProvider.GetInstalledSdks();

            return installedSdks.Any(sdk => sdk.StartsWith(requiredSdkVersion + ".") || sdk == requiredSdkVersion);
        }

        private string GetSdkVersionFromMoniker(RuntimeMoniker runtimeMoniker)
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