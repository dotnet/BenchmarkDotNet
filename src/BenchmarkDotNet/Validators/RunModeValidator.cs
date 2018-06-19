using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

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
                    int invocationCount = run.InvocationCount;
                    if (invocationCount % unrollFactor != 0)
                    {
                        string message = $"Specified InvocationCount ({invocationCount}) must be a multiple of UnrollFactor ({unrollFactor})";
                        yield return new ValidationError(true, message, benchmark);
                    }
                }
                
                int minTargetCount = run.ResolveValue(RunMode.MinWorkloadIterationCountCharacteristic, resolver);
                int maxTargetCount = run.ResolveValue(RunMode.MaxWorkloadIterationCountCharacteristic, resolver);

                if (minTargetCount <= 0)
                    yield return new ValidationError(true, $"{nameof(RunMode.MinTargetIterationCount)} must be greater than zero (was {minTargetCount})", benchmark);

                if (maxTargetCount <= 0)
                    yield return new ValidationError(true, $"{nameof(RunMode.MaxTargetIterationCount)} must be greater than zero (was {maxTargetCount})", benchmark);

                if (minTargetCount >= maxTargetCount)
                    yield return new ValidationError(true, $"{nameof(RunMode.MaxTargetIterationCount)} must be greater than {nameof(RunMode.MinTargetIterationCount)} (was {maxTargetCount} and {minTargetCount})", benchmark);
            }
        }
    }
}