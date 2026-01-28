using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ExecutionValidator : ExecutionValidatorBase
    {
        public static readonly ExecutionValidator DontFailOnError = new ExecutionValidator(false);
        public static readonly ExecutionValidator FailOnError = new ExecutionValidator(true);

        private ExecutionValidator(bool failOnError)
            : base(failOnError) { }

        protected override async ValueTask ExecuteBenchmarksAsync(object benchmarkTypeInstance, IEnumerable<BenchmarkCase> benchmarks, List<ValidationError> errors)
        {
            foreach (var benchmark in benchmarks)
            {
                try
                {
                    var result = benchmark.Descriptor.WorkloadMethod.Invoke(benchmarkTypeInstance, null);
                    await DynamicAwaitHelper.GetOrAwaitResult(result);
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Failed to execute benchmark '{benchmark.DisplayInfo}', exception was: '{GetDisplayExceptionMessage(ex)}'",
                        benchmark));
                }
            }
        }
    }
}