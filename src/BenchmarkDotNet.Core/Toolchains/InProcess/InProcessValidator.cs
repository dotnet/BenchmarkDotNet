using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    ///     Validator to be used together with <see cref="InProcessToolchain" />
    ///     to proof that the config matches the environment.
    /// </summary>
    /// <seealso cref="IValidator" />
    [PublicAPI]
    public class InProcessValidator : IValidator
    {
        // ReSharper disable HeapView.DelegateAllocation
        private static readonly IReadOnlyDictionary<Characteristic, Func<Job, Characteristic, string>> ValidationRules =
            new Dictionary<Characteristic, Func<Job, Characteristic, string>>
            {
                { EnvMode.AffinityCharacteristic, DontValidate },
                { EnvMode.JitCharacteristic, ValidateEnvironment },
                { EnvMode.PlatformCharacteristic, ValidatePlatform },
                { EnvMode.RuntimeCharacteristic, ValidateEnvironment },
                { GcMode.ServerCharacteristic, ValidateEnvironment },
                { GcMode.ConcurrentCharacteristic, ValidateEnvironment },
                { GcMode.CpuGroupsCharacteristic, ValidateEnvironment },
                { GcMode.ForceCharacteristic, DontValidate },
                { GcMode.AllowVeryLargeObjectsCharacteristic, DontValidate },
                { RunMode.LaunchCountCharacteristic, DontValidate },
                { RunMode.RunStrategyCharacteristic, DontValidate },
                { RunMode.WarmupCountCharacteristic, DontValidate },
                { RunMode.TargetCountCharacteristic, DontValidate },
                { RunMode.IterationTimeCharacteristic, DontValidate },
                { RunMode.InvocationCountCharacteristic, DontValidate },
                { RunMode.UnrollFactorCharacteristic, DontValidate },
                { AccuracyMode.AnalyzeLaunchVarianceCharacteristic, DontValidate },
                { AccuracyMode.EvaluateOverheadCharacteristic, DontValidate },
                { AccuracyMode.MaxRelativeErrorCharacteristic, DontValidate },
                { AccuracyMode.MaxAbsoluteErrorCharacteristic, DontValidate },
                { AccuracyMode.MinInvokeCountCharacteristic, DontValidate },
                { AccuracyMode.MinIterationTimeCharacteristic, DontValidate },
                { AccuracyMode.RemoveOutliersCharacteristic, DontValidate },
                { InfrastructureMode.ClockCharacteristic, DontValidate },
                { InfrastructureMode.EngineFactoryCharacteristic, DontValidate },
                { InfrastructureMode.ToolchainCharacteristic, ValidateToolchain }
            };

        // ReSharper restore HeapView.DelegateAllocation

        private static string DontValidate(Job job, Characteristic characteristic) => null;

        private static string ValidateEnvironment(Job job, Characteristic characteristic)
        {
            var resolver = EnvResolver.Instance;
            var actual = resolver.Resolve(Job.Default, characteristic);
            var expected = resolver.Resolve(job, characteristic);
            return Equals(actual, expected)
                ? null
                : $"was run as {actual} ({expected} expected). Fix your test runner options.";
        }

        private static string ValidatePlatform(Job job, Characteristic characteristic)
        {
            if (job.Env.Platform == Platform.AnyCpu)
                return null;

            return ValidateEnvironment(job, characteristic);
        }

        private static string ValidateToolchain(Job job, Characteristic characteristic) =>
            job.Infrastructure.Toolchain is InProcessToolchain
                ? null
                : $"should be instance of {nameof(InProcessToolchain)}.";

        /// <summary>The instance of validator that does NOT fail on error.</summary>
        public static readonly IValidator DontFailOnError = new InProcessValidator(false);

        /// <summary>The instance of validator that DOES fail on error.</summary>
        public static readonly IValidator FailOnError = new InProcessValidator(true);

        public static bool IsSupported(Benchmark benchmark, ILogger logger)
        {
            var result = new List<ValidationError>();
            ValidateJob(benchmark.Job, true, result);
            if (result.Any())
            {
                logger.WriteLineInfo($"// Benchmark {benchmark.DisplayInfo}");
                logger.WriteLineInfo("// cannot be run in-process. Validation errors:");
                foreach (var validationError in result)
                {
                    logger.WriteLineInfo($"//    * {validationError.Message}");
                }
                logger.WriteLine();

                return false;
            }
            return true;
        }

        private static void ValidateJob(Job job, bool isCritical, ICollection<ValidationError> validationResults)
        {
            foreach (var characteristic in job.GetCharacteristicsWithValues())
            {
                Func<Job, Characteristic, string> validationRule;
                if (ValidationRules.TryGetValue(characteristic, out validationRule))
                {
                    var message = validationRule(job, characteristic);
                    if (!string.IsNullOrEmpty(message))
                        validationResults.Add(
                            new ValidationError(
                                isCritical,
                                $"Job {job}, {characteristic.FullId} {message}"));
                }
#if DEBUG
                else if (characteristic.IsPresentableCharacteristic())
                {
                    validationResults.Add(
                        new ValidationError(
                            false,
                            $"Job {job}, {characteristic.FullId}: no validation rule specified."));
                }
#endif
            }
        }


        private InProcessValidator(bool failOnErrors)
        {
            TreatsWarningsAsErrors = failOnErrors;
        }

        /// <summary>Gets a value indicating whether warnings are treated as errors.</summary>
        /// <value>
        ///     <c>true</c> if the validator should treat warnings as errors; otherwise, <c>false</c>.
        /// </value>
        public bool TreatsWarningsAsErrors { get; }

        /// <summary>Proofs that benchmarks' jobs match the environment.</summary>
        /// <param name="validationParameters">The validation parameters.</param>
        /// <returns>Enumerable of validation errors.</returns>
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var result = new List<ValidationError>();

            var targets = validationParameters.Benchmarks.Select(b => b.Target).Distinct();
            foreach (var target in targets)
            {
                if (!string.IsNullOrEmpty(target.AdditionalLogic))
                    result.Add(
                        new ValidationError(
                            false,
                            $"Target {target} has {nameof(target.AdditionalLogic)} filled. AdditionalLogic is not supported by in-process toolchain."));
            }

            foreach (var job in validationParameters.Config.GetJobs())
            {
                ValidateJob(job, TreatsWarningsAsErrors, result);
            }

            return result.ToArray();
        }
    }
}