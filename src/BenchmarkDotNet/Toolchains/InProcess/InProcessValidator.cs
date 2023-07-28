using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Validators;

using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    ///     Validator to be used together with <see cref="InProcessNoEmitToolchain" /> or <see cref="InProcessEmitToolchain"/>
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
                { EnvironmentMode.AffinityCharacteristic, DontValidate },
                { EnvironmentMode.JitCharacteristic, ValidateEnvironment },
                { EnvironmentMode.PlatformCharacteristic, ValidatePlatform },
                { EnvironmentMode.RuntimeCharacteristic, ValidateEnvironment },
                { GcMode.ServerCharacteristic, ValidateEnvironment },
                { GcMode.ConcurrentCharacteristic, ValidateEnvironment },
                { GcMode.CpuGroupsCharacteristic, ValidateEnvironment },
                { GcMode.NoAffinitizeCharacteristic, ValidateEnvironment },
                { GcMode.HeapAffinitizeMaskCharacteristic, DontValidate },
                { GcMode.HeapCountCharacteristic, DontValidate },
                { GcMode.ForceCharacteristic, DontValidate },
                { GcMode.AllowVeryLargeObjectsCharacteristic, DontValidate },
                { RunMode.LaunchCountCharacteristic, DontValidate },
                { RunMode.RunStrategyCharacteristic, DontValidate },
                { RunMode.WarmupCountCharacteristic, DontValidate },
                { RunMode.IterationCountCharacteristic, DontValidate },
                { RunMode.IterationTimeCharacteristic, DontValidate },
                { RunMode.InvocationCountCharacteristic, DontValidate },
                { RunMode.UnrollFactorCharacteristic, DontValidate },
                { AccuracyMode.AnalyzeLaunchVarianceCharacteristic, DontValidate },
                { AccuracyMode.EvaluateOverheadCharacteristic, DontValidate },
                { AccuracyMode.MaxRelativeErrorCharacteristic, DontValidate },
                { AccuracyMode.MaxAbsoluteErrorCharacteristic, DontValidate },
                { AccuracyMode.MinInvokeCountCharacteristic, DontValidate },
                { AccuracyMode.MinIterationTimeCharacteristic, DontValidate },
                { AccuracyMode.OutlierModeCharacteristic, DontValidate },
                { InfrastructureMode.ClockCharacteristic, DontValidate },
                { InfrastructureMode.EngineFactoryCharacteristic, DontValidate },
                { InfrastructureMode.ToolchainCharacteristic, ValidateToolchain }
            };

        // ReSharper restore HeapView.DelegateAllocation

        private static string DontValidate(Job job, Characteristic characteristic) => null;

        private static string ValidateEnvironment(Job job, Characteristic characteristic)
        {
            var resolver = EnvironmentResolver.Instance;
            var actual = resolver.Resolve(Job.Default, characteristic);
            var expected = resolver.Resolve(job, characteristic);
            return Equals(actual, expected)
                ? null
                : $"was run as {actual} ({expected} expected). Fix your test runner options.";
        }

        private static string ValidatePlatform(Job job, Characteristic characteristic)
        {
            if (job.Environment.Platform == Platform.AnyCpu)
                return null;

            return ValidateEnvironment(job, characteristic);
        }

        private static string ValidateToolchain(Job job, Characteristic characteristic) =>
            job.Infrastructure.Toolchain is InProcessEmitToolchain
            || job.Infrastructure.Toolchain is InProcessNoEmitToolchain
                ? null
                : $"should be instance of {nameof(InProcessEmitToolchain)} or {nameof(InProcessNoEmitToolchain)}.";

        /// <summary>The instance of validator that does NOT fail on error.</summary>
        public static readonly IValidator DontFailOnError = new InProcessValidator(false);

        /// <summary>The instance of validator that DOES fail on error.</summary>
        public static readonly IValidator FailOnError = new InProcessValidator(true);

        public static IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase)
        {
            foreach (var validationError in ValidateJob(benchmarkCase.Job, true))
            {
                yield return new ValidationError(
                    validationError.IsCritical,
                    validationError.Message,
                    benchmarkCase);
            }

            if (benchmarkCase.HasArguments)
                yield return new ValidationError(true, "Arguments are not supported by the InProcessToolchain yet, see #687 for more details", benchmarkCase);
        }

        private static IEnumerable<ValidationError> ValidateJob(Job job, bool isCritical)
        {
            foreach (var characteristic in job.GetCharacteristicsWithValues())
            {
                if (ValidationRules.TryGetValue(characteristic, out var validationRule))
                {
                    string message = validationRule(job, characteristic);
                    if (!string.IsNullOrEmpty(message))
                        yield return new ValidationError(
                                isCritical,
                                $"Job {job}, {characteristic.FullId} {message}");
                }
#if DEBUG
                else if (characteristic.IsPresentableCharacteristic())
                {
                    yield return new ValidationError(
                        false,
                        $"Job {job}, {characteristic.FullId}: no validation rule specified.");
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
            foreach (var target in validationParameters.Benchmarks
                .Where(benchmark => !string.IsNullOrEmpty(benchmark.Descriptor.AdditionalLogic))
                .Select(b => b.Descriptor)
                .Distinct())
            {
                yield return new ValidationError(
                    false,
                    $"Target {target} has {nameof(target.AdditionalLogic)} filled. AdditionalLogic is not supported by in-process toolchain.");
            }

            foreach (var benchmarkWithArguments in validationParameters.Benchmarks.Where(benchmark => benchmark.HasArguments && benchmark.GetToolchain() is InProcessNoEmitToolchain))
                yield return new ValidationError(true, "Arguments are not supported by the InProcessNoEmitToolchain, see #687 for more details", benchmarkWithArguments);

            foreach (var validationError in validationParameters.Config.GetJobs().SelectMany(job => ValidateJob(job, TreatsWarningsAsErrors)))
                yield return validationError;
        }
    }
}