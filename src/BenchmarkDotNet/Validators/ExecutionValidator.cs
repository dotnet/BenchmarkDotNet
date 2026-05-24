using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Validators
{
    public class ExecutionValidator : ExecutionValidatorBase
    {
        public static readonly ExecutionValidator DontFailOnError = new(false);
        public static readonly ExecutionValidator FailOnError = new(true);

        private ExecutionValidator(bool failOnError)
            : base(failOnError) { }

        protected override async IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var errors = new List<ValidationError>();

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (!TryCreateBenchmarkTypeInstance(benchmark.Descriptor.Type, errors, cancellationToken, out var benchmarkTypeInstance))
                {
                    continue;
                }
                if (!TryFillParamsAndGetArgs(benchmark, benchmarkTypeInstance, errors, out var args, cancellationToken))
                {
                    continue;
                }
                if (await TryToCallSetupOrCleanup<GlobalSetupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.GlobalSetupMethod, errors, cancellationToken).ConfigureAwait(true))
                {
                    if (await TryToCallSetupOrCleanup<IterationSetupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.IterationSetupMethod, errors, cancellationToken).ConfigureAwait(true))
                    {
                        await ExecuteBenchmarkAsync(benchmarkTypeInstance, benchmark, args, errors, cancellationToken).ConfigureAwait(true);
                        await TryToCallSetupOrCleanup<IterationCleanupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.IterationCleanupMethod, errors, cancellationToken).ConfigureAwait(true);
                    }
                    await TryToCallSetupOrCleanup<GlobalCleanupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.GlobalCleanupMethod, errors, cancellationToken).ConfigureAwait(true);
                }
            }

            foreach (var error in errors)
            {
                yield return error;
            }
        }

        private async ValueTask ExecuteBenchmarkAsync(object benchmarkTypeInstance, BenchmarkCase benchmark, object?[]? args, List<ValidationError> errors, CancellationToken cancellationToken)
        {
            try
            {
                var workloadMethod = benchmark.Descriptor.WorkloadMethod;
                var result = workloadMethod.Invoke(benchmarkTypeInstance, args);
                if (workloadMethod.ReturnType.IsAwaitable(out var awaitableInfo))
                {
                    if (result is null)
                    {
                        errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Awaitable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                        return;
                    }
                    await DynamicAwaitHelper.AwaitResult(result, awaitableInfo).ConfigureAwait(false);
                }
                else if (workloadMethod.ReturnType.IsAsyncEnumerable(out var asyncEnumerableInfo))
                {
                    if (result is null)
                    {
                        errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Async enumerable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                        return;
                    }
                    await DynamicAwaitHelper.DrainAsyncEnumerableAsync(result, asyncEnumerableInfo).ConfigureAwait(false);
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