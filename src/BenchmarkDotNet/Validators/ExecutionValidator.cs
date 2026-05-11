using BenchmarkDotNet.Extensions;
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

        protected override async ValueTask ExecuteBenchmarksAsync(object benchmarkTypeInstance, IEnumerable<BenchmarkCase> benchmarks, List<ValidationError> errors, CancellationToken cancellationToken)
        {
            foreach (var benchmark in benchmarks)
            {
                try
                {
                    var workloadMethod = benchmark.Descriptor.WorkloadMethod;
                    var result = workloadMethod.Invoke(benchmarkTypeInstance, null);
                    if (workloadMethod.ReturnType.IsAwaitable())
                    {
                        if (result is null)
                        {
                            errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Awaitable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                            continue;
                        }
                        await DynamicAwaitHelper.AwaitResult(result, workloadMethod.ReturnType).ConfigureAwait(true);
                    }
                    else if (workloadMethod.ReturnType.IsAsyncEnumerable(out _, out _, out _))
                    {
                        if (result is null)
                        {
                            errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Async enumerable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                            continue;
                        }
                        await DynamicAwaitHelper.DrainAsyncEnumerableAsync(result, workloadMethod.ReturnType).ConfigureAwait(true);
                    }
                }
                catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
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