using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class RunModeValidator : IValidator
    {
        public static readonly IValidator FailOnError = new RunModeValidator();

        private RunModeValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var resolver = new CompositeResolver(EnvironmentResolver.Instance, EngineResolver.Instance); // TODO: use specified resolver.
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var run = benchmark.Job.Run;
                int unrollFactor = run.ResolveValue(RunMode.UnrollFactorCharacteristic, resolver);
                if (unrollFactor <= 0)
                {
                    yield return new ValidationError(true, $"Specified UnrollFactor ({unrollFactor}) must be greater than zero", benchmark);
                }
                else if (run.HasValue(RunMode.InvocationCountCharacteristic))
                {
                    long invocationCount = run.InvocationCount;
                    if (invocationCount % unrollFactor != 0)
                    {
                        string message = $"Specified InvocationCount ({invocationCount}) must be a multiple of UnrollFactor ({unrollFactor})";
                        yield return new ValidationError(true, message, benchmark);
                    }
                }

                foreach (var validationError in ValidateMinMax(run, resolver, benchmark, RunMode.MinIterationCountCharacteristic, RunMode.MaxIterationCountCharacteristic))
                    yield return validationError;

                foreach (var validationError in ValidateMinMax(run, resolver, benchmark, RunMode.MinWarmupIterationCountCharacteristic, RunMode.MaxWarmupIterationCountCharacteristic))
                    yield return validationError;
            }
        }

        private static IEnumerable<ValidationError> ValidateMinMax(RunMode run, CompositeResolver resolver, BenchmarkCase benchmark,
            Characteristic<int> minCharacteristic, Characteristic<int> maxCharacteristic)
        {
            string GetName(Characteristic characteristic) => $"{characteristic.DeclaringType.Name}.{characteristic.Id}";

            int minCount = run.ResolveValue(minCharacteristic, resolver);
            int maxCount = run.ResolveValue(maxCharacteristic, resolver);

            if (minCount <= 0)
                yield return new ValidationError(true, $"{GetName(minCharacteristic)} must be greater than zero (was {minCount})", benchmark);

            if (maxCount <= 0)
                yield return new ValidationError(true, $"{GetName(maxCharacteristic)} must be greater than zero (was {maxCount})", benchmark);

            if (minCount >= maxCount)
                yield return new ValidationError(true, $"{GetName(maxCharacteristic)} must be greater than {GetName(minCharacteristic)} (was {maxCount} and {minCount})", benchmark);
        }
    }
}