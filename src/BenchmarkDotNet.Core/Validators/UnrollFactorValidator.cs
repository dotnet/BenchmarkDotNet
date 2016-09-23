using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class UnrollFactorValidator : IValidator
    {
        public static readonly IValidator Default = new UnrollFactorValidator();

        private UnrollFactorValidator()
        {
        }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(IList<Benchmark> benchmarks)
        {
            var resolver = EnvResolver.Instance; // TODO: use specified resolver.
            foreach (var benchmark in benchmarks)
            {
                var run = benchmark.Job.Run;
                int unrollFactor = run.UnrollFactor.Resolve(resolver);
                if (unrollFactor <= 0)
                {
                    string message = $"Specified UnrollFactor ({unrollFactor}) must be greater than zero";
                    yield return new ValidationError(true, message, benchmark);
                }
                else if (!run.InvocationCount.IsDefault)
                {
                    int invocationCount = run.InvocationCount.SpecifiedValue;
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