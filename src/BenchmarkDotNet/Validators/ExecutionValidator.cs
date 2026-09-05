using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
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
                if (await TryToCallSetupOrCleanup<GlobalSetupAttribute>(benchmark, benchmarkTypeInstance, benchmark.Descriptor.GlobalSetupMethod, errors, cancellationToken).ConfigureAwait())
                {
                    if (await TryToCallSetupOrCleanup<IterationSetupAttribute>(benchmark, benchmarkTypeInstance, benchmark.Descriptor.IterationSetupMethod, errors, cancellationToken).ConfigureAwait())
                    {
                        await ExecuteBenchmarkAsync(benchmarkTypeInstance, benchmark, args, errors, cancellationToken).ConfigureAwait();
                        await TryToCallSetupOrCleanup<IterationCleanupAttribute>(benchmark, benchmarkTypeInstance, benchmark.Descriptor.IterationCleanupMethod, errors, cancellationToken).ConfigureAwait();
                    }
                    await TryToCallSetupOrCleanup<GlobalCleanupAttribute>(benchmark, benchmarkTypeInstance, benchmark.Descriptor.GlobalCleanupMethod, errors, cancellationToken).ConfigureAwait();
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
                if (workloadMethod.ReturnType.WithoutRefModifier().IsByRefLike())
                {
                    errors.Add(new ValidationError(false, $"Benchmark '{benchmark.DisplayInfo}' returns by-ref-like value, skipping execution validation.", benchmark));
                    return;
                }
                if (workloadMethod.ReturnType.IsAwaitable(out var awaitableInfo))
                {
                    var result = workloadMethod.Invoke(benchmarkTypeInstance, args);
                    if (result is null)
                    {
                        errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Awaitable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                        return;
                    }
                    await DynamicAwaitHelper.AwaitResult(result, awaitableInfo).ConfigureAwait(false);
                }
                else if (workloadMethod.ReturnType.IsAsyncEnumerable(out var asyncEnumerableInfo))
                {
                    if (asyncEnumerableInfo.CurrentProperty.PropertyType.IsByRefLike())
                    {
                        errors.Add(new ValidationError(false, $"Async enumerable benchmark '{benchmark.DisplayInfo}' yields by-ref-like elements, skipping execution validation.", benchmark));
                        return;
                    }
                    var result = workloadMethod.Invoke(benchmarkTypeInstance, args);
                    if (result is null)
                    {
                        errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Async enumerable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                        return;
                    }
                    // Mirrors real benchmark execution.
                    await foreach (var item in DynamicAwaitHelper.EnumerateBenchmarkAsync(result, asyncEnumerableInfo).ConfigureAwait(false))
                    {
                        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(item);
                    }
                }
                else
                {
                    var result = workloadMethod.Invoke(benchmarkTypeInstance, args);
                    DeadCodeEliminationHelper.KeepAliveWithoutBoxing(result);
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