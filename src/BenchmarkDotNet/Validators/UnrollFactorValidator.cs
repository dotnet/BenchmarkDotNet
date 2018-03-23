using System.Collections.Generic;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Validators
{
    public class UnrollFactorValidator : IValidator
    {
        public static readonly IValidator Default = new UnrollFactorValidator();

        private UnrollFactorValidator()
        {
        }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var resolver = EnvResolver.Instance; // TODO: use specified resolver.
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var run = benchmark.Job.Run;
                int unrollFactor = run.ResolveValue(RunMode.UnrollFactorCharacteristic, resolver);
                if (unrollFactor <= 0)
                {
                    string message = $"Specified UnrollFactor ({unrollFactor}) must be greater than zero";
                    yield return new ValidationError(true, message, benchmark);
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
            }
        }
    }
}